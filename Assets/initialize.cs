using System.Collections;
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
    public static readonly int NUMBER_OF_NODES = 13;
    public static List<GameNode> nodes = new List<GameNode>(Manager.NUMBER_OF_NODES);
    //public static List<GameTimer> timers = new List<GameTimer>();

    /*public static GameTimer addTimer(float totalCountdown) {
        GameTimer timer = new GameTimer(totalCountdown);
        timers.Add(timer);
        return timer;
    }*/
}


//public class GameTimer {
//    float totalCountdown;
//    public GameTimer(float totalCountdown) {
//        this.totalCountdown = totalCountdown;
//    }

//    public void update() {
//        totalCountdown -= Time.deltaTime;

//        if (totalCountdown <= 0) {
//            Manager.timers.Remove(this);
//        }
//    }

//    public Action OverridableMethod { get; set; }

//}

public class GameNode : ScriptableObject {
    public GameObject thisNode;
    public List<GameNode> connections = new List<GameNode>();
    public int nodeIndex;
    public bool isVirus;
    public Constants.Shape thisShape;

    public void init(int nodeIndex, Constants.Shape shape, bool isVirus) {
        thisNode = GameObject.Find("node" + nodeIndex);
        this.nodeIndex = nodeIndex;
        thisShape = shape;
        switch (shape) {
            case Constants.Shape.CIRCLE:
                thisNode.GetComponent<Renderer>().material.color = Color.blue;
                break;
            case Constants.Shape.SQUARE:
                thisNode.GetComponent<Renderer>().material.color = Color.yellow;
                break;

        }
        this.isVirus = isVirus;
        if (this.isVirus) {
            thisNode.GetComponent<Renderer>().material.color = Color.red;
        }
        //thisNode.renderer.material.color = new Color(1, 1, 1);
    }

    public void addConnection(GameNode newConnection, float weight) {
        connections.Add(newConnection);

        // TODO add weight
    }

    public void removeConnection(GameNode oldConnection) {
        connections.Remove(oldConnection);
    }

    public void spawnPacketStream() {
        //GameObject newGOPacketStream = new GameObject();
        //PacketStream newPacketStream = newGOPacketStream.AddComponent<PacketStream>();
        //newPacketStream.init(this);
        new PacketStream(this);
    }

    public void die() {
        // remove connections
        for(int i = 0; i < connections.Count; i++) {
            connections[i].removeConnection(this);
        }

        // explode


        thisNode.GetComponent<Renderer>().material.color = Color.black;
        // remove node
        Manager.nodes.Remove(this);

		Camera camera = Camera.main;
		float shade = 0.5f * Manager.nodes.Count / Manager.NUMBER_OF_NODES + 0.5f;
		camera.backgroundColor = new Color(shade, shade, shade);
    }

    public void playerKilled() {

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
            GameObject.Find("sfxClickAttempt").GetComponent<AudioSource>().Play();

            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                if (hit.transform.gameObject == thisNode) {
                    selected();
                }
            }
        }
    }
}

public class PacketStream {
    public List<GameObject> packets = new List<GameObject>();
    private List<GameNode> currentDestination = new List<GameNode>();
    private GameNode originalStartingNode;
    private GameNode currentStartingNode;
    private List<GameNode> packetStreamPath = new List<GameNode>();
    private static int MAX_PATH_LENGTH = 5;
    private static int PACKET_LENGTH = 4;
    private int hitLastDestinationCount = 0;
    private List<float> times = new List<float>();
    private bool killGameNode;

    public PacketStream(GameNode startingNode) {
        originalStartingNode = startingNode;
        // add this packet stream to list
        Manager.packetStreams.Add(this);
        
        for (int i = 0; i < PACKET_LENGTH; i++) {
            currentDestination.Add(null);
        }

        packetStreamPath.Add(startingNode);
        pickNextDestination(startingNode, 0); // first destination for leader

        // if start node is virus, then this stream is also a virus
        killGameNode = originalStartingNode.isVirus;

        for (int i = 0; i < PACKET_LENGTH; i++) {
            times.Add(i * 0.15f);
            //times.Add(i);
        }
    }

    public void sendNewPacket() {
//        var primitive = PrimitiveType.Sphere;
//        switch (originalStartingNode.thisShape) {
//            case Constants.Shape.CIRCLE:
//                primitive = PrimitiveType.Sphere;
//                break;
//            case Constants.Shape.SQUARE:
//                primitive = PrimitiveType.Cube;
//                break;
//
//        }


		GameObject packet = new GameObject();
		MeshFilter meshFilter = packet.AddComponent<MeshFilter>();
		meshFilter.sharedMesh = originalStartingNode.thisNode.GetComponent<MeshFilter>().mesh;
		MeshRenderer meshRenderer = packet.AddComponent<MeshRenderer>();
		meshRenderer.material = originalStartingNode.thisNode.GetComponent<Renderer>().material;

        switch (originalStartingNode.thisShape) {
            case Constants.Shape.CIRCLE:
                packet.GetComponent<Renderer>().material.color = Color.blue;
                break;
            case Constants.Shape.SQUARE:
                packet.GetComponent<Renderer>().material.color = Color.yellow;
                break;
        }

        packets.Add(packet); // head

        packet.transform.localScale += new Vector3(1f, 1f, 1f);
        int currentIndex = packets.Count - 1;
        if (currentIndex > 0) {
            currentDestination[currentIndex] = currentDestination[currentIndex - 1];
        }

        packets[currentIndex].transform.position = Utils.cloneVector(GameObject.Find("node" + originalStartingNode.nodeIndex).transform.position);
    }

    public void pickNextDestination(GameNode startingNode, int index) {
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
            if (connectionOptions.Count == 0 || packetStreamPath.Count >= MAX_PATH_LENGTH) {
                packetHitLastDestination();
            } else {
                if (!killGameNode && currentDestination[index] != null) {
                    killGameNode = currentDestination[index].isVirus;
                }
                float random = Random.Range(0.0f, connectionOptions.Count - 1);
                currentDestination[index] = (connectionOptions[Mathf.RoundToInt(random)]);
                packetStreamPath.Add(currentDestination[index]);
            }
        }
        
    }

    public void packetHitLastDestination() {
        hitLastDestinationCount++;
    }

    public void update() {
        for (int i = times.Count - 1; i >= 0; i--) {
            times[i] -= Time.deltaTime;
            if (times[i] <= 0) {
                times.RemoveAt(i);
                sendNewPacket();
            }
        }
        if (hitLastDestinationCount == 0 || packets.Count > hitLastDestinationCount) {
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
        } else {
            if (killGameNode) {
                // kill dying node
                packetStreamPath[packetStreamPath.Count - 1].die();
                GameObject.Find("sfxExplosion").GetComponent<AudioSource>().Play();
            }
            packets.ForEach(delegate (GameObject packet) {
                GameObject.Destroy(packet);
            });
            Manager.packetStreams.Remove(this);
        }
    }
}

public class initialize : MonoBehaviour {
    //public List<GameNode> nodes = new List<GameNode>(40);
    public static readonly float TIME_BETWEEN_PACKET_STREAMS_IN_S = 2f;

    private float timeUntilNextPacketStream = 0;

    public void addConnection(GameNode node1, GameNode node2, float weight) {
        node1.addConnection(node2, weight);
        node2.addConnection(node1, weight);
    }

    public void addConnection(int index1, int index2, float weight) {
        addConnection(Manager.nodes[index1], Manager.nodes[index2], weight);

    }

    // Use this for initialization
    void Start() {
        Dictionary<int, Constants.Shape> shapeDict = new Dictionary<int, Constants.Shape>() {
            { 0, Constants.Shape.CIRCLE },
            { 1, Constants.Shape.CIRCLE },
            { 2, Constants.Shape.CIRCLE },
            { 3, Constants.Shape.SQUARE },
            { 4, Constants.Shape.SQUARE },
            { 5, Constants.Shape.SQUARE },
            { 6, Constants.Shape.SQUARE },
            { 7, Constants.Shape.SQUARE },
            { 8, Constants.Shape.SQUARE },
            { 9, Constants.Shape.SQUARE },
            { 10, Constants.Shape.SQUARE },
            { 11, Constants.Shape.SQUARE },
            { 12, Constants.Shape.SQUARE }
        };

        int virusNode = Mathf.RoundToInt(Random.Range(0, Manager.NUMBER_OF_NODES - 1));

        // create nodes
        for (int i = 0; i < Manager.NUMBER_OF_NODES; i++) {
            Manager.nodes.Add(null);
        }

        // find nodes
        for (int i = 0; i < Manager.NUMBER_OF_NODES; i++) {
            Manager.nodes[i] = ScriptableObject.CreateInstance<GameNode>();
            Manager.nodes[i].init(i, shapeDict[i], virusNode == i);
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
        
        addConnection(3, 5, 0.25f);
        addConnection(8, 6, 0.25f);

        addConnection(3, 11, 0.25f);
        addConnection(4, 8, 0.25f);
        addConnection(8, 11, 0.25f);

        addConnection(12, 9, 0.25f);
        addConnection(7, 12, 0.25f);
        addConnection(2, 12, 0.25f);
        addConnection(10, 12, 0.25f);


        // create lines
        Manager.nodes.ForEach(delegate (GameNode node) {
            if (node) {
                node.createLines();
            }
        });
    }

    // Update is called once per frame
    void Update() {

        Manager.nodes.ForEach(delegate (GameNode node) {
            if (node) {
                node.update();
            }
        });

        Manager.packetStreams.ForEach(delegate (PacketStream packetStream) {
            if (packetStream != null) {
                packetStream.update();
            }
        });

        //Manager.timers.ForEach(delegate (GameTimer timer) {
        //    timer.update();
        //});

        timeUntilNextPacketStream -= Time.deltaTime;

        if (timeUntilNextPacketStream <= 0) {
            timeUntilNextPacketStream = TIME_BETWEEN_PACKET_STREAMS_IN_S;
            int nodeIndex = Mathf.RoundToInt(Random.Range(0, Manager.nodes.Count));
            Manager.nodes[nodeIndex].spawnPacketStream();
        }
    }

    /*public class Tap : MonoBehaviour {

        void OnEnable() {
            Cardboard.SDK.OnTrigger += TriggerPulled;
        }

        void OnDisable() {
            Cardboard.SDK.OnTrigger -= TriggerPulled;
        }

        void TriggerPulled() {
            Debug.Log("The trigger was pulled!");
        }
    }*/
}