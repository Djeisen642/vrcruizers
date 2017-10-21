﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Constants {
    public enum Shape { SQUARE, CIRCLE, PYRAMID };
}

public static class Utils {
    public static Vector3 cloneVector(Vector3 orig) {
        return new Vector3(orig.x, orig.y, orig.z);
    }
}

public static class Manager {
    public static List<PacketStream> packetStreams = new List<PacketStream>();
}

public class GameNode : ScriptableObject {
    public GameObject thisNode;
    public List<GameNode> connections = new List<GameNode>();
    public int nodeIndex;
    public Constants.Shape thisShape;

    public void init(int nodeIndex, Constants.Shape shape) {
        thisNode = GameObject.Find("node" + nodeIndex);
        this.nodeIndex = nodeIndex;
        thisShape = shape;
        //thisNode.renderer.material.color = new Color(1, 1, 1);
    }

    public void addConnection(GameNode newConnection, float weight) {
        connections.Add(newConnection);

        // TODO add weight
    }

    public void spawnPacketStream() {
        GameObject newGOPacketStream = new GameObject();
        PacketStream newPacketStream = newGOPacketStream.AddComponent<PacketStream>();
        newPacketStream.init(this);
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

    public void selected() {
        thisNode.GetComponent<Renderer>().material.color = Color.green;
    }

    public void update() {
        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hitInfo = new RaycastHit();
            bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, Camera.main.nearClipPlane)), out hitInfo);

            if (hit) {
                if (hitInfo.transform.gameObject == thisNode) {
                    selected();
                }
            }
        }
    }
}

public class PacketStream : MonoBehaviour {
    public List<GameObject> packets = new List<GameObject>();
    private List<GameNode> currentDestination = new List<GameNode>();
    private GameNode currentStartingNode;
    private List<GameNode> packetStreamPath = new List<GameNode>();
    private static int MAX_PATH_LENGTH = 5;
    private static int PACKET_LENGTH = 4;
    private int hitLastDestinationCount = 0;

    public void init(GameNode startingNode) {
        // add this packet stream to list
        Manager.packetStreams.Add(this);

        for (int i = 0; i < PACKET_LENGTH; i++) {
            currentDestination.Add(null);
        }

        packetStreamPath.Add(startingNode);
        pickNextDestination(startingNode, 0); // first destination for leader

        for (int i = 0; i < PACKET_LENGTH; i++) {
            StartCoroutine(sendNewPacket(i));
        }
        
    }

    public IEnumerator sendNewPacket(int index) {
        yield return new WaitForSeconds(index * 0.3f);
        GameObject packet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        packet.transform.localScale -= new Vector3(0.5f, 0.5f, 0.5f);
        packets.Add(packet); // head
        int currentIndex = packets.Count - 1;
        if (currentIndex > 0) {
            currentDestination[currentIndex] = currentDestination[currentIndex - 1];
        }
        
        packets[currentIndex].transform.position = Utils.cloneVector(currentStartingNode.thisNode.transform.position);
    }

    public void pickNextDestination(GameNode startingNode, int index) {
        //Debug.Log(index);
        if (index > 0) { // follower
            if (currentDestination[index] == currentDestination[0]) {
                packetHitLastDestination();
            } else {
                currentDestination[index] = currentDestination[index - 1];
            }
        } else { // leader
            currentStartingNode = startingNode;
            List<GameNode> connectionOptions = new List<GameNode>(startingNode.connections);
            // remove used paths
            for (int i = 0; i < packetStreamPath.Count; i++) {
                int nodeIndexValue = packetStreamPath[i].nodeIndex;
                for (int j = 0; j < connectionOptions.Count; j++) {
                    if (connectionOptions[j].nodeIndex == nodeIndexValue) {
                        connectionOptions.RemoveAt(j);
                        break;
                    }
                }
            }

            // no paths in options, forced to stop
            Debug.Log(connectionOptions.Count);
            if (connectionOptions.Count == 0 || packetStreamPath.Count >= MAX_PATH_LENGTH) {
                packetHitLastDestination();
            } else {
                float random = Random.Range(0.0f, connectionOptions.Count - 1);
                currentDestination[index] = (connectionOptions[(int)(Mathf.Round(random))]);
                packetStreamPath.Add(currentDestination[index]);
            }
        }
        
    }

    public void packetHitLastDestination() {
        hitLastDestinationCount++;
    }

    public void update() {
        for (int i = 0; i < packets.Count; i++) {
            if (i >= hitLastDestinationCount) {
                GameObject packet = packets[i];
                packet.transform.position = Vector3.MoveTowards(packet.transform.position, currentDestination[i].thisNode.transform.position, 0.2f);
                float dist = Vector3.Distance(packet.transform.position, currentDestination[i].thisNode.transform.position);
                if (Mathf.Approximately(dist, 0)) {
                    pickNextDestination(currentDestination[i], i);
                }
            }
        }
    }


}

public class initialize : MonoBehaviour {
    private static readonly int NUMBER_OF_NODES = 6;
    public List<GameNode> nodes = new List<GameNode>(40);

    public void addConnection(GameNode node1, GameNode node2, float weight) {
        node1.addConnection(node2, weight);
        node2.addConnection(node1, weight);
    }

    public void addConnection(int index1, int index2, float weight) {
        addConnection(nodes[index1], nodes[index2], weight);
    }

    // Use this for initialization
    void Start() {
        Dictionary<int, Constants.Shape> shapeDict = new Dictionary<int, Constants.Shape>() {
            { 0, Constants.Shape.CIRCLE },
            { 1, Constants.Shape.CIRCLE },
            { 2, Constants.Shape.CIRCLE },
            { 3, Constants.Shape.SQUARE },
            { 4, Constants.Shape.SQUARE },
            { 5, Constants.Shape.SQUARE }
        };

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

        // connect spheres
        addConnection(0, 1, 0.25f);
        addConnection(1, 2, 0.25f);
        addConnection(2, 3, 0.5f);

        addConnection(3, 4, 0.25f);
        addConnection(3, 5, 0.25f);


        // create lines
        nodes.ForEach(delegate (GameNode node) {
            if (node) {
                node.createLines();
            }
        });
    }

    // Update is called once per frame
    void Update() {
        nodes.ForEach(delegate (GameNode node) {
            if (node) {
                node.update();
            }
        });

        Manager.packetStreams.ForEach(delegate (PacketStream packetStream) {
            if (packetStream != null) {
                packetStream.update();
            }
        });

        if (Input.GetMouseButtonDown(0)) {
            nodes[0].spawnPacketStream();
        }

    }
}
