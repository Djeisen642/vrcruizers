using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Change animation type to legacy

public class Constants {
	public enum GameState { ONGOING, WIN, GAMEOVER };
    public enum Shape { CUBE, SPHERE, DIAMOND };
}

public static class Utils {
    public static Vector3 cloneVector(Vector3 orig) {
        return new Vector3(orig.x, orig.y, orig.z);
    }
}

public static class Manager {
    public static List<PacketStream> packetStreams = new List<PacketStream>();
    public static readonly int NUMBER_OF_NODES = 25;
    public static List<GameNode> nodes = new List<GameNode>(Manager.NUMBER_OF_NODES);
	public static GameNode virusNode;
	public static readonly float LOSS_CONDITION = 0.5f;
	public static Constants.GameState gameState = Constants.GameState.ONGOING;
	public static int DEAD_NODE_LOSS_CONDITION = Mathf.RoundToInt(LOSS_CONDITION * NUMBER_OF_NODES);
    public static GameObject youWin, youLose, virusKillsSelf;

    public static void gameFinishedWithImage(GameObject GameObjectImage) {
        Camera.main.transform.LookAt(virusNode.thisNode.transform.position);
        GameObjectImage.SetActive(true);
    }

    public static void checkLossAndDealWithIt() {
		int deadNodes = NUMBER_OF_NODES - nodes.Count;
		if (deadNodes >= Manager.DEAD_NODE_LOSS_CONDITION) { // lost
			Debug.Log("Lost");
			gameState = Constants.GameState.GAMEOVER;
			virusNode.showVirus();
            gameFinishedWithImage(youLose);
		} else {
			float percentageRemaining = LOSS_CONDITION * nodes.Count / NUMBER_OF_NODES;
			Debug.Log ("Check percentage " + percentageRemaining * LOSS_CONDITION);
			Camera camera = Camera.main;
			float shade = percentageRemaining + 0.5f;
			camera.backgroundColor = new Color(shade, shade, shade);
			if (Manager.virusNode.connections.Count == 0) {
				Debug.Log("Virus killed self");
				gameState = Constants.GameState.WIN;
				virusNode.showVirus();
                gameFinishedWithImage(virusKillsSelf);
			}
		}
	}

	public static void checkWinAndDealWithIt(GameNode selectedGameNode) {
		Debug.Log (selectedGameNode.isVirus);
		if (selectedGameNode.isVirus) {
			Debug.Log("Picked Correctly");
			gameState = Constants.GameState.WIN;
            gameFinishedWithImage(youWin);
        } else {
			Debug.Log("Bad pick");
		}
		selectedGameNode.playerKilled();
	}
}

public class GameNode : ScriptableObject {
    public List<GameNode> connections = new List<GameNode>();
    public int nodeIndex;
    public bool isVirus;
	public Constants.Shape thisShape;
	public GameObject thisNode;
	private Color savedColor;

    public void init(int nodeIndex, Constants.Shape shape, bool isVirus) {
        thisNode = GameObject.Find("node" + nodeIndex);
        this.nodeIndex = nodeIndex;
        thisShape = shape;
        switch (shape) {
            case Constants.Shape.SPHERE:
				savedColor = Color.blue;
                break;
            case Constants.Shape.CUBE:
				savedColor = Color.yellow;
                break;
            case Constants.Shape.DIAMOND:
				savedColor = Color.green;
                break;
        }

		thisNode.GetComponent<Renderer>().material.color = savedColor;

        this.isVirus = isVirus;
        if (this.isVirus) {
            showVirus();
        }
    }

    public void addConnection(GameNode newConnection, float weight) {
        connections.Add(newConnection);

        // TODO add weight
    }

    public void removeConnection(GameNode oldConnection) {
        connections.Remove(oldConnection);
    }

    public void spawnPacketStream() {
        new PacketStream(this);
    }

    public void die() {
        // remove connections
        for(int i = 0; i < connections.Count; i++) {
            connections[i].removeConnection(this);
        }

		explode();

        thisNode.GetComponent<Renderer>().material.color = Color.black;
        // remove node
        Manager.nodes.Remove(this);

		Manager.checkLossAndDealWithIt();
    }

	public void implode() {
		// explode
		AudioClip clip = Resources.Load("Audio/explosion") as AudioClip;
		AudioSource.PlayClipAtPoint(clip, Utils.cloneVector(thisNode.transform.position));
	}

	public void explode() {
		// explode
		AudioClip clip = Resources.Load("Audio/explosion") as AudioClip;
		AudioSource.PlayClipAtPoint(clip, Utils.cloneVector(thisNode.transform.position));
	}

	public void showVirus() {
		thisNode.GetComponent<Renderer>().material.color = Color.red;
	}

    public void playerKilled() {
		if (isVirus) {
			// implode
			implode();
		} else {
			// explode
			for(int i = 0; i < connections.Count; i++) {
				if (!connections [i].isVirus) {
					connections[i].die ();
				}
			}
			die();

		}
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

	public Color getCurrentColor() {
		return thisNode.GetComponent<Renderer> ().material.color;
	}

	public void highlighted() {
		if (getCurrentColor () != Color.magenta) {
			savedColor = getCurrentColor ();
		}
		thisNode.GetComponent<Renderer>().material.color = Color.magenta;
	}

	public void resetColor() {
		if (savedColor != getCurrentColor ()) {
			thisNode.GetComponent<Renderer> ().material.color = savedColor;
		}
	}

    public void selected() {
		Manager.checkWinAndDealWithIt(this);
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
    private static float PACKET_SPEED = 0.1f;
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
        }
    }

    public void sendNewPacket() {
		GameObject packet = new GameObject();
		MeshFilter meshFilter = packet.AddComponent<MeshFilter>();
		meshFilter.sharedMesh = originalStartingNode.thisNode.GetComponent<MeshFilter>().mesh;
		MeshRenderer meshRenderer = packet.AddComponent<MeshRenderer>();
		meshRenderer.material = originalStartingNode.thisNode.GetComponent<Renderer>().material;

        switch (originalStartingNode.thisShape) {
            case Constants.Shape.SPHERE:
                packet.GetComponent<Renderer>().material.color = Color.blue;
                break;
            case Constants.Shape.CUBE:
                packet.GetComponent<Renderer>().material.color = Color.yellow;
                break;
            case Constants.Shape.DIAMOND:
                packet.GetComponent<Renderer>().material.color = Color.green;
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
                    packet.transform.position = Vector3.MoveTowards(packet.transform.position, currentDestination[i].thisNode.transform.position, PACKET_SPEED);
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
            }
            packets.ForEach(delegate (GameObject packet) {
                GameObject.Destroy(packet);
            });
            Manager.packetStreams.Remove(this);
        }
    }
}

public class initialize : MonoBehaviour {
    public static readonly float TIME_BETWEEN_PACKET_STREAMS_IN_S = 1f;

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
        Manager.youWin = GameObject.Find("youWin");
        Manager.youWin.SetActive(false);
        Manager.youLose = GameObject.Find("youLose");
        Manager.youLose.SetActive(false);
        Manager.virusKillsSelf = GameObject.Find("virusKillsSelf");
        Manager.virusKillsSelf.SetActive(false);

        Dictionary<int, Constants.Shape> shapeDict = new Dictionary<int, Constants.Shape>() {
            { 0, Constants.Shape.SPHERE },
            { 1, Constants.Shape.SPHERE },
            { 2, Constants.Shape.SPHERE },
            { 3, Constants.Shape.SPHERE },
            { 4, Constants.Shape.SPHERE },
            { 5, Constants.Shape.SPHERE },
            { 6, Constants.Shape.SPHERE },
            { 7, Constants.Shape.SPHERE },
            { 8, Constants.Shape.CUBE },
            { 9, Constants.Shape.CUBE },
            { 10, Constants.Shape.CUBE },
            { 11, Constants.Shape.CUBE },
            { 12, Constants.Shape.CUBE },
            { 13, Constants.Shape.CUBE },
            { 14, Constants.Shape.CUBE },
            { 15, Constants.Shape.CUBE },
            { 16, Constants.Shape.CUBE },
            { 17, Constants.Shape.DIAMOND },
            { 18, Constants.Shape.DIAMOND },
            { 19, Constants.Shape.DIAMOND },
            { 20, Constants.Shape.DIAMOND },
            { 21, Constants.Shape.DIAMOND },
            { 22, Constants.Shape.DIAMOND },
            { 23, Constants.Shape.DIAMOND },
            { 24, Constants.Shape.DIAMOND }
        };

        int virusNodeIndex = Mathf.RoundToInt(Random.Range(0, Manager.NUMBER_OF_NODES - 1));

        // create nodes
        for (int i = 0; i < Manager.NUMBER_OF_NODES; i++) {
            Manager.nodes.Add(null);
        }

        // find nodes
        for (int i = 0; i < Manager.NUMBER_OF_NODES; i++) {
            Manager.nodes[i] = ScriptableObject.CreateInstance<GameNode>();
			Manager.nodes[i].init(i, shapeDict[i], virusNodeIndex == i);
            /*Rigidbody rb = node.GetComponent<Rigidbody>();
            System.Random random = new System.Random();
            rb.velocity = new Vector3((float)(random.NextDouble())/2, (float) (random.NextDouble())/2, (float)(random.NextDouble())/2); // range 0.0 to 1.0*/
        }

		Manager.virusNode = Manager.nodes[virusNodeIndex];

        //node1 = GameObject.Find("node1");
        //Rigidbody rb = node1.GetComponent<Rigidbody>();
        //rb.velocity = new Vector3(0, 0.5f, 0);

        // add some connection
        addConnection(22, 13, 1f);
        addConnection(19, 22, 1f);
        addConnection(15, 13, 1f);
        addConnection(22, 21, 1f);
        addConnection(21, 17, 1f);
        addConnection(17, 18, 1f);
        addConnection(23, 2, 1f);
        addConnection(4, 5, 1f);
        addConnection(10, 13, 1f);
        addConnection(10, 9, 1f);
        addConnection(16, 11, 1f);
        addConnection(11, 14, 1f);
        addConnection(12, 14, 1f);
        addConnection(12, 13, 1f);
        addConnection(12, 15, 1f);
        addConnection(8, 11, 1f);
        addConnection(11, 6, 1f);
        addConnection(6, 0, 1f);
        addConnection(3, 6, 1f);
        addConnection(3, 0, 1f);
        addConnection(22, 10, 1f);
        addConnection(14, 15, 1f);
        addConnection(8, 9, 1f);
        addConnection(8, 1, 1f);
        addConnection(1, 5, 1f);
        addConnection(2, 7, 1f);
        addConnection(23, 24, 1f);
        addConnection(4, 20, 1f);
        addConnection(5, 7, 1f);
        addConnection(1, 3, 1f);
        addConnection(19, 24, 1f);
        addConnection(20, 17, 1f);
        addConnection(17, 23, 1f);
        addConnection(23, 20, 1f);

        // create lines
        Manager.nodes.ForEach(delegate (GameNode node) {
            if (node) {
                node.createLines();
            }
        });
    }

    // Update is called once per frame
    void Update() {
		if (Manager.gameState != Constants.GameState.ONGOING) {
			// TODO GAMEOVER banner
			// TODO Winner banner
			return;
		}
		GameObject selectedObject = null;

		bool clicked = false;

		if (Input.GetMouseButtonDown (0)) {
			clicked = true;
		}



		Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit)) {
			if (clicked) {
				GameObject.Find("sfxClickAttempt").GetComponent<AudioSource>().Play();
			}
			selectedObject = hit.transform.gameObject;
//			Debug.Log (selectedObject.name);
		}
		else
			Debug.Log("I'm looking at nothing!");



		Manager.nodes.ForEach (delegate (GameNode node) {
			if (selectedObject != null) {
				if (node.thisNode == selectedObject) {
//					Debug.Log(node.nodeIndex);
					if (clicked) {
						node.selected();
					} else {
						node.highlighted();
					}
				} else {
					node.resetColor();
				}
			} else {
				node.resetColor();
			}
		});
        

        Manager.packetStreams.ForEach(delegate (PacketStream packetStream) {
            if (packetStream != null) {
                packetStream.update();
            }
        });

        timeUntilNextPacketStream -= Time.deltaTime;

        if (timeUntilNextPacketStream <= 0) {
            timeUntilNextPacketStream = TIME_BETWEEN_PACKET_STREAMS_IN_S;
            int nodeIndex = Mathf.RoundToInt(Random.Range(0, Manager.nodes.Count));
            Manager.nodes[nodeIndex].spawnPacketStream();
        }
    }
}