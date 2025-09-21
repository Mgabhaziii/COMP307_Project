using System.Collections.Generic;
using UnityEngine;

public class pathFinder : MonoBehaviour
{
    [SerializeField] private GameObject player;

    private Vector3 playerPos;
    private Vector3 strightPath;
    public List<Vector3> path;

    private GameObject[] allGameObjects;
    private List<GameObject> pathObsticles;

    private void Start()
    {
        allGameObjects = GameObject.FindObjectsOfType<GameObject>();
        path = new List<Vector3>();
        pathObsticles = new List<GameObject>();
    }

    void FixedUpdate()
    {
        playerPos = player.transform.position;

        // Reset obstacle list
        pathObsticles.Clear();
        setPath(transform.position);

        // Run pathfinding
        FindPath(transform.position, playerPos);
    }

    void setPath(Vector3 startPath)
    {
        strightPath = playerPos - startPath;

        float minX = Mathf.Min(startPath.x, startPath.x + strightPath.x);
        float maxX = Mathf.Max(startPath.x, startPath.x + strightPath.x);
        float minY = Mathf.Min(startPath.y, startPath.y + strightPath.y);
        float maxY = Mathf.Max(startPath.y, startPath.y + strightPath.y);
        float minZ = Mathf.Min(startPath.z, startPath.z + strightPath.z);
        float maxZ = Mathf.Max(startPath.z, startPath.z + strightPath.z);

        foreach (GameObject obj in allGameObjects)
        {
            if (obj == gameObject || obj == player) continue; // skip self and player

            if (obj.transform.position.x >= minX - 1 &&
                obj.transform.position.y >= minY - 1 &&
                obj.transform.position.z >= minZ - 1 &&
                obj.transform.position.x <= maxX + 1 &&
                obj.transform.position.y <= maxY + 1 &&
                obj.transform.position.z <= maxZ + 1)
            {
                pathObsticles.Add(obj);
            }
        }
    }

    // Pathfinding //

    class Node
    {
        public Vector3 worldPos;
        public bool walkable;
        public int gCost; // distance from start
        public int hCost; 
        public Node parent;

        public int fCost => gCost + hCost;

        public Node(Vector3 pos, bool walkable)
        {
            this.worldPos = pos;
            this.walkable = walkable;
            gCost = int.MaxValue;
            hCost = 0;
        }
    }

    void FindPath(Vector3 startPos, Vector3 targetPos)
    {
        float gridSize = 1f; // resolution of grid
        int searchRadius = 20; // how far to search

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        Dictionary<Vector3, Node> allNodes = new Dictionary<Vector3, Node>();

        Node GetNode(Vector3 pos)
        {
            pos = RoundToGrid(pos, gridSize);
            if (!allNodes.ContainsKey(pos))
            {
                bool walkable = true;
                foreach (GameObject obs in pathObsticles)
                {
                    if (Vector3.Distance(obs.transform.position, pos) < 0.5f)
                    {
                        walkable = false;
                        break;
                    }
                }
                allNodes[pos] = new Node(pos, walkable);
            }
            return allNodes[pos];
        }

        Node startNode = GetNode(startPos);
        Node targetNode = GetNode(targetPos);

        startNode.gCost = 0;
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < current.fCost ||
                    (openSet[i].fCost == current.fCost && openSet[i].hCost < current.hCost))
                {
                    current = openSet[i];
                }
            }

            openSet.Remove(current);
            closedSet.Add(current);

            if (current == targetNode)
            {
                RetracePath(startNode, targetNode);
                return;
            }

            foreach (Node neighbor in GetNeighbors(current, gridSize, searchRadius, GetNode))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor)) continue;

                int newCost = current.gCost + (int)Vector3.Distance(current.worldPos, neighbor.worldPos);
                if (newCost < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newCost;
                    neighbor.hCost = (int)Vector3.Distance(neighbor.worldPos, targetNode.worldPos);
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        // No path found
        path.Clear();
    }

    Vector3 RoundToGrid(Vector3 pos, float size)
    {
        return new Vector3(
            Mathf.Round(pos.x / size) * size,
            Mathf.Round(pos.y / size) * size,
            Mathf.Round(pos.z / size) * size
        );
    }

    List<Node> GetNeighbors(Node node, float gridSize, int radius, System.Func<Vector3, Node> GetNode)
    {
        List<Node> neighbors = new List<Node>();
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0) continue;
                Vector3 newPos = node.worldPos + new Vector3(x * gridSize, 0, z * gridSize);
                if (Vector3.Distance(newPos, node.worldPos) <= radius)
                    neighbors.Add(GetNode(newPos));
            }
        }
        return neighbors;
    }

    void RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> newPath = new List<Vector3>();
        Node current = endNode;

        while (current != null && current != startNode)
        {
            newPath.Add(current.worldPos);
            current = current.parent;
        }
        newPath.Reverse();

        path.Clear();
        path.AddRange(newPath);
    }

    private void OnDrawGizmos()
    {
        if (path != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
                Gizmos.DrawSphere(path[i], 0.1f);
            }
        }
    }
}

