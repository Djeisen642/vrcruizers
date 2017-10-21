using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Constants {
    public enum Shape { SQUARE, CIRCLE, PYRAMID };
}

public class GameNode : ScriptableObject  {
    public GameObject thisNode;
    public List<GameNode> connections;
    public int nodeIndex;
    public Constants.Shape thisShape;

    public void init(int nodeIndex, Constants.Shape shape) {
        thisNode = GameObject.Find("node" + nodeIndex);
        this.nodeIndex = nodeIndex;
        thisShape = shape;
        connections = new List<GameNode>();
        //thisNode.renderer.material.color = new Color(1, 1, 1);
    }

    public void addConnection(GameNode newConnection) {
        connections.Add(newConnection);
    }

    public void die() {
        
    }

    public void createLines() {
        connections.ForEach(delegate (GameNode connection) {
            if (connection.nodeIndex < nodeIndex) {
                Vector3 finalPosition = connection.thisNode.transform.position;
                
                GameObject line = new GameObject();
                LineRenderer newLine = line.AddComponent<LineRenderer>();
                newLine.startWidth = 0.3f;
                newLine.endWidth = 0.3f;
                newLine.SetPosition(0, connection.thisNode.transform.position);
                newLine.SetPosition(1, thisNode.transform.position);
                newLine.material = Resources.Load("Materials/StandardMaterial") as Material;
            }
        });
    }

    public void update() {
        if (Input.GetMouseButtonDown(0)) {

            RaycastHit hitInfo = new RaycastHit();
            bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);

            if (hit) {
                if (hitInfo.transform.gameObject == thisNode) {
                    thisNode.GetComponent<Renderer>().material.color = Color.green;
                }
                else {
                }
            }
        }



    }

    public void render() {
        
    }

    
}

public class initialize : MonoBehaviour {
    private static readonly int NUMBER_OF_NODES = 3;
    public List<GameNode> nodes = new List<GameNode>(40);

    public void addConnection(GameNode node1, GameNode node2) {
        node1.addConnection(node2);
        node2.addConnection(node1);
    }

    public void addConnection(int index1, int index2) {
        addConnection(nodes[index1], nodes[index2]);
    }

    // Use this for initialization
    void Start() {
        Dictionary<int, Constants.Shape> shapeDict = new Dictionary<int, Constants.Shape>() {
            { 0, Constants.Shape.CIRCLE },
            { 1, Constants.Shape.SQUARE },
            { 2, Constants.Shape.SQUARE }
        };
        List<int> circleShapes = new List<int>() { 0 };
        List<int> squareShapes = new List<int>() { 1, 2 };

        // find nodes
        for (int i = 0; i < NUMBER_OF_NODES; i++) {
            nodes[i] = ScriptableObject.CreateInstance<GameNode>();
            nodes[i].init(i, shapeDict[i]);
            /*Rigidbody rb = node.GetComponent<Rigidbody>();
            System.Random random = new System.Random();
            rb.velocity = new Vector3((float)(random.NextDouble())/2, (float) (random.NextDouble())/2, (float)(random.NextDouble())/2); // range 0.0 to 1.0*/
        }

        //node1 = GameObject.Find("node1");
        //Rigidbody rb = node1.GetComponent<Rigidbody>();
        //rb.velocity = new Vector3(0, 0.5f, 0);

        // add some connection
        addConnection(0, 1);

        // create lines
        nodes.ForEach(delegate (GameNode node) {
            if (node) {
                node.createLines();
            }
        });
    }
	
	// Update is called once per frame
	void Update () {
        nodes.ForEach(delegate (GameNode node) {
            if (node) {
                node.update();
            }
        });
    }
}
