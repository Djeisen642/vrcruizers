using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameNode : MonoBehaviour{
    public GameObject thisNode;
    public List<GameNode> connections;
    public int nodeIndex;

    public GameNode(int nodeIndex) {
        thisNode = GameObject.Find("node" + nodeIndex);
        this.nodeIndex = nodeIndex;
        connections = new List<GameNode>();
    }

    public void addConnection(GameNode newConnection) {
        connections.Add(newConnection);
    }

    public void die() {
        
    }

    public void createLines() {
        connections.ForEach(delegate (GameNode connection) {
            if (connection.nodeIndex < nodeIndex) {
                GameObject line = new GameObject();
                LineRenderer newLine = line.AddComponent<LineRenderer>();
                newLine.startWidth = 0.1f;
                newLine.endWidth = 0.1f;
                newLine.SetPosition(0, connection.thisNode.transform.position);
                newLine.SetPosition(1, thisNode.transform.position);
            }
        });
    }

    public void update() {
        

        
        //line.materials[0].mainTextureScale = new Vector3(distance, 1, 1);
    }

    public void render() {
        
    }

    
}

public class initialize : MonoBehaviour {
    private static readonly int NUMBER_OF_NODES = 3;
    public GameNode[] nodes = new GameNode[40];

    public void addConnection(GameNode node1, GameNode node2) {
        node1.addConnection(node2);
        node2.addConnection(node1);
    }

    public void addConnection(int index1, int index2) {
        addConnection(nodes[index1], nodes[index2]);
    }

    // Use this for initialization
    void Start () {
        // find nodes
        for (int i = 0; i < NUMBER_OF_NODES; i++) {
            nodes[i] = new GameNode(i);
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
        for (int i = 0; i < NUMBER_OF_NODES; i++) {
            nodes[i].createLines();
        }
    }
	
	// Update is called once per frame
	void Update () {
        for (int i = 0; i < NUMBER_OF_NODES; i++) {
            nodes[i].update();
            //nodes[i].transform.Translate(Vector3.right * 0.5f * Time.deltaTime);
        }

    }
}
