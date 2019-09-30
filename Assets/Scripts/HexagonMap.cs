using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Hexagon.Mini
{
    public class HexagonMap : MonoBehaviour
    {
        public int MapSize = 5;

        public float CellSize = 0.56f;

        public float MoveCost { get; private set; }

        [SerializeField]
        private GameObjectEntity _tileEntityPrefab;

        // -x , y
        private List<HexagonNode> _topLeftNodes = new List<HexagonNode>();
        // x , y
        private List<HexagonNode> _topRightNodes = new List<HexagonNode>();
        // -x , -y
        private List<HexagonNode> _botLeftNodes = new List<HexagonNode>();
        // x , -y
        private List<HexagonNode> _botRightNodes = new List<HexagonNode>();



        private WaitForSeconds _wait = new WaitForSeconds(1.5f);

        public void Start()
        {
            StartCoroutine(GenerateNodes());
        }

        private IEnumerator GenerateNodes()
        {
            yield return StartCoroutine(GenerateNodeRecursive(HexagonNode.zero));
            _topLeftNodes.Clear();
            _topLeftNodes = null;
            _topRightNodes.Clear();
            _topRightNodes = null;
            _botLeftNodes.Clear();
            _botLeftNodes = null;
            _botRightNodes.Clear();
            _botRightNodes = null;
        }

        private IEnumerator GenerateNodeRecursive(HexagonNode node)
        {
            yield return new WaitForSeconds(0.075f);
            GameObjectEntity nodeEntity = Instantiate(_tileEntityPrefab);
            nodeEntity.transform.position = new Vector2(node.X, node.Y);
            AddNodes(node);

            HexagonNode curNode;
            Vector2[] adjacentNodes = node.GetAdjacentNodes(CellSize, MapSize);
            for ( int i = 0; i < adjacentNodes.Length; i++ )
            {
                curNode = new HexagonNode(adjacentNodes[i].x, adjacentNodes[i].y);
                if ( SearchNodeByPosition(curNode) )
                {
                    continue;
                }
                yield return StartCoroutine(GenerateNodeRecursive(curNode));
            }
        }

        public static bool CheckInsideHexagon(float x, float y, float cellSize, float size)
        {
            float maxX = size * (0.75f * cellSize);
            if ( x < 0 )
            {
                float maxY = size * cellSize + x * ((float)2 / 3);
                if ( y < 0 )
                {
                    return (Mathf.Approximately(y, -maxY) || y > -maxY) && x >= -maxX;
                }
                else
                {
                    return (Mathf.Approximately(y, maxY) || y < maxY) && x >= -maxX;
                }
            }
            else
            {
                float maxY = size * cellSize - x * ((float)2 / 3);
                if ( y < 0 )
                {
                    return (Mathf.Approximately(y, -maxY) || y > -maxY) && x <= maxX;
                }
                else
                {
                    return (Mathf.Approximately(y, maxY) || y < maxY) && x <= maxX;
                }
            }
        }

        private bool SearchNodeByPosition(HexagonNode cell)
        {
            return GetNodesByPos(cell).Contains(cell);
        }

        private void AddNodes(HexagonNode node)
        {
            if ( node.X < 0 )
            {
                if ( node.Y < 0 )
                {
                    _botLeftNodes.Add(node);
                }
                else
                {
                    _topLeftNodes.Add(node);
                }
            }
            else
            {
                if ( node.Y < 0 )
                {
                    _botRightNodes.Add(node);
                }
                else
                {
                    _topRightNodes.Add(node);
                }
            }
        }

        private List<HexagonNode> GetNodesByPos(HexagonNode node)
        {
            if ( node.X < 0 )
            {
                if ( node.Y < 0 )
                {
                    return _botLeftNodes;
                }
                else
                {
                    return _topLeftNodes;
                }
            }
            else
            {
                if ( node.Y < 0 )
                {
                    return _botRightNodes;
                }
                else
                {
                    return _topRightNodes;
                }
            }
        }
    }

    public class HexagonNode : IComparable<HexagonNode>
    {
        public float X;
        public float Y;

        public HexagonNode(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static HexagonNode operator -(HexagonNode a, HexagonNode b)
        {
            return new HexagonNode(a.X - b.X, a.Y - b.Y);
        }

        public float Magnitude
        {
            get
            {
                return Mathf.Sqrt(SqrMagnitude);
            }
        }

        public float SqrMagnitude
        {
            get
            {
                return X * X + Y * Y;
            }
        }

        public static readonly HexagonNode zero = new HexagonNode(0, 0);

        public int CompareTo(HexagonNode other)
        {
            int d;
            d = X.CompareTo(other.X);
            if ( d != 0 )
            {
                return d;
            }
            d = Y.CompareTo(other.Y);
            if ( d != 0 )
            {
                return d;
            }
            return 0;

        }

        public static bool operator ==(HexagonNode a, HexagonNode b)
        {
            return Equals(a, b);
        }

        public static bool operator !=(HexagonNode a, HexagonNode b)
        {
            return !Equals(a, b);
        }

        public static bool Equals(HexagonNode a, HexagonNode b)
        {
            return Mathf.Abs(a.X - b.X) < Mathf.Epsilon
                    && Mathf.Abs(a.Y - b.Y) < Mathf.Epsilon;
        }

        public override bool Equals(object obj)
        {
            if ( obj == null || obj.GetType() != typeof(HexagonNode) )
            {
                return false;
            }
            return Equals(this, (HexagonNode)obj);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() >> 1;
        }

        public Vector2[] GetAdjacentNodes(float cellSize, float mapSize)
        {
            return GetAdjacentNodesByPos(X, Y, cellSize, mapSize);
        }

        public static Vector2[] GetAdjacentNodesByPos(Vector2 pos, float cellSize, float mapSize)
        {
            return GetAdjacentNodesByPos(pos.x, pos.y, cellSize, mapSize);
        }

        public static Vector2[] GetAdjacentNodesByPos(float x, float y, float cellSize, float mapSize)
        {
            float nextTopY = (float)Math.Round(y + cellSize, 2);
            float nextBotY = (float)Math.Round(y - cellSize, 2);
            float nextLeftX = (float)Math.Round(x - 0.75f * cellSize, 2);
            float nextSideBotY = (float)Math.Round(y - (cellSize * 0.5f), 2);
            float nextRightX = (float)Math.Round(x + 0.75f * cellSize, 2);
            float nextSideTopY = (float)Math.Round(y + (cellSize * 0.5f), 2);

            List<Vector2> adjacentNodes = new List<Vector2>();

            if ( HexagonMap.CheckInsideHexagon(x, nextTopY, cellSize, mapSize) )
            {
                adjacentNodes.Add(new Vector2(x, nextTopY));
            }

            if ( HexagonMap.CheckInsideHexagon(x, nextBotY, cellSize, mapSize) )
            {
                adjacentNodes.Add(new Vector2(x, nextBotY));
            }

            if ( HexagonMap.CheckInsideHexagon(nextLeftX, nextSideTopY, cellSize, mapSize) )
            {
                adjacentNodes.Add(new Vector2(nextLeftX, nextSideTopY));
            }

            if ( HexagonMap.CheckInsideHexagon(nextLeftX, nextSideBotY, cellSize, mapSize) )
            {
                adjacentNodes.Add(new Vector2(nextLeftX, nextSideBotY));
            }

            if ( HexagonMap.CheckInsideHexagon(nextRightX, nextSideTopY, cellSize, mapSize) )
            {
                adjacentNodes.Add(new Vector2(nextRightX, nextSideTopY));
            }

            if ( HexagonMap.CheckInsideHexagon(nextRightX, nextSideBotY, cellSize, mapSize) )
            {
                adjacentNodes.Add(new Vector2(nextRightX, nextSideBotY));
            }

            return adjacentNodes.ToArray();
        }

    }
}
