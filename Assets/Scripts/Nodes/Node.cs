using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Node : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	public Graph graph;

	public int id;
	public Text namefield;

	public bool calculate = false;

    public bool infected = false;
    public bool endangered = false;
    public float timeOfInfection;

    public bool highlighted;
	public bool changingColor;

	public bool hidden;
	public bool hideconnections;
	public bool dontRepel;

	public Vector3 savedPosition;

	public Vector3 forceVelocity;
    public Vector3 masterVelocity = Vector3.zero;
	public Vector3 throwVelocity;

    public bool evilMaster;

	public List<Node> repulsionlist = new List<Node> ();
	public List<Edge> attractionlist = new List<Edge> ();
    public List<Node> connectedNodes = new List<Node>();

    public GameObject grabbedBy;

	public static Node CreateNode (Graph graph, int id, string name)
	{
		GameObject newNodeGO = GameObject.Instantiate (GraphImporter.instance.nodePrefab) as GameObject;
		newNodeGO.transform.SetParent (graph.transform);
		newNodeGO.name = name;
		Node newNode = newNodeGO.GetComponent<Node> ();
		newNode.id = id;

		newNode.graph = graph;
		graph.nodes.Add (newNode);

		return newNode;
	}

	#region showandhide

	public void Hide ()
	{
		hidden = true;
		GetComponent<Renderer> ().enabled = false;
		GetComponent<SphereCollider> ().enabled = false;

		CanvasGroup cv = GetComponent<CanvasGroup> ();
		cv.alpha = 0;
		cv.blocksRaycasts = false;
		cv.interactable = false;
	}

	public void Show ()
	{
		hidden = false;
		GetComponent<Renderer> ().enabled = true;

		GetComponent<SphereCollider> ().enabled = true;

		CanvasGroup cv = GetComponent<CanvasGroup> ();
		cv.alpha = 1;
		cv.blocksRaycasts = true;
		cv.interactable = true;
		namefield.transform.localPosition = Vector3.zero;			
	}

	public void CheckGrab ()
	{

		if (graph.leftController.GetComponent<ViveGrab>().grabbedObj == gameObject) { // GRABBED BY LEFT
			grabbedBy = graph.leftController.gameObject;
		} else if (graph.rightController.GetComponent<ViveGrab>().grabbedObj == gameObject) { // GRABBED BY RIGHT
			grabbedBy = graph.rightController.gameObject;
		} else // NOT GRABBED
			grabbedBy = null;
	}


	public void HideConnections ()
	{
		hideconnections = true;
	}

	public void ShowConnections ()
	{
		hideconnections = false;
	}

	public void Remove ()
	{
		dontRepel = true;
		HideConnections ();
		Invoke ("Destroy", 1.5f);
	}

	protected virtual void Destroy () // IS INVOKED
	{
		Destroy (gameObject);
	}

	#endregion

	#region events


	public virtual void OnPointerEnter (PointerEventData eventData)
	{
		Debug.Log ("Enter" + name);
	}

	public virtual void OnPointerExit (PointerEventData eventData)
	{
		Debug.Log ("Exit" + name);
	}

	public virtual void OnPointerClick (PointerEventData eventData)
	{
		
	}

	public virtual void OnBeginDrag (PointerEventData eventData)
	{
	}

	public virtual void OnDrag (PointerEventData eventData)
	{
		//transform.position = Camera.main.ScreenToWorldPoint (new Vector3 (Input.mousePosition.x, Input.mousePosition.y, 0));
	}

	public virtual void OnEndDrag (PointerEventData eventData)
	{
		
	}

	#endregion events


	#region physics

	protected Vector3 CalcAttractionToCenter ()
	{
		Vector3 a = transform.position;
		Vector3 b = graph.center.transform.position;
		return (b - a).normalized * graph.attraction * (Vector3.Distance (a, b) / graph.springLength);
	}

	protected Vector3 CalcAttraction (Node otherNode, float weight)
	{
		if (otherNode) {

			    Vector3 a = transform.localPosition;
			    Vector3 b = otherNode.transform.localPosition;

                float springLength = graph.springLength;
                float springAttraction = graph.attraction;

                if (otherNode.infected)
                { 
                    springLength = graph.springLength / 3f;
                    springAttraction = graph.attraction * 2f;
                }

                return (b - a).normalized * (graph.attraction + weight) * (Vector3.Distance (a, b) / springLength);

        } else
			return Vector3.zero;
	}

	protected Vector3 CalcRepulsion (Node otherNode)
	{
        if (!dontRepel)
        {
            // Coulomb's Law: F = k(Qq/r^2)
            float distance = Vector3.Distance(transform.localPosition, otherNode.transform.localPosition);
            Vector3 returnvector = ((transform.localPosition - otherNode.transform.localPosition).normalized * graph.repulsion) / (distance * distance);

            if (!float.IsNaN(returnvector.x) && !float.IsNaN(returnvector.y) && !float.IsNaN(returnvector.z))
                return returnvector;
            else
                return Vector3.zero;
        }
        else
            return Vector3.zero;
    }

    #endregion physics


    public void FixedInfection()
    {
        graph.infectedNodes.Add(this);
        infected = true;
        timeOfInfection = Time.time;
        Debug.Log("INFECTION: " + name + " at " + timeOfInfection);
    }

    public void TryToInfect()
    {

        if (!infected && Random.value > graph.infection.chance)
        {
            graph.infectedNodes.Add(this);
            infected = true;
            timeOfInfection = Time.time;
            Debug.Log("Infected " + name);
        }
    }


    public void Heal()
    {
        Debug.Log("Healed " + name);
        infected = false;
        GetComponent<Renderer>().material = graph.materialImmune;

    }


	public void CheckHighlight ()
	{
        if(graph.evilMaster == this)
        {
            GetComponent<Renderer>().material = graph.materialEvilMaster;
            GetComponent<Light>().color = Color.red;
        }
        else if (graph.infectedNodes.Contains(this))
        {
            GetComponent<Renderer>().material = graph.materialInfected;
            GetComponent<Light>().color = Color.red;
        }
        else if(graph.evilNodes.Contains(this) && !graph.evilMaster == this)
        { 
            GetComponent<Renderer>().material = graph.materialEvil;
            GetComponent<Light>().color = Color.black;
        }
        else if(!infected && endangered)
        {
            GetComponent<Renderer>().material.Lerp(graph.materialStandard, graph.materialEndangered, Mathf.FloorToInt(Time.time) % 2);
            GetComponent<Light>().color = Color.Lerp(Color.yellow, Color.red, Mathf.FloorToInt(Time.time) % 2);
        }
        else if(!infected && !endangered)
        {
            //if (grabbedBy == null && highlighted)
            //{ // REVERT HIGHLIGHTS
            //    highlighted = false;
            //    Debug.Log("Resetting " + name);
            //    StartCoroutine(ChangeNodeColor(graph.materialHighlighted, graph.materialStandard));
            //}
            //else if (grabbedBy != null && !highlighted)
            //{ // SET HIGHLIGHTS
            //    highlighted = true;
            //    Debug.Log("Highlighting " + name);
            //    StartCoroutine(ChangeNodeColor(graph.materialStandard, graph.materialHighlighted));
            //}

            if(grabbedBy != null)
            {
                GetComponent<Renderer>().material = graph.materialHighlighted;
                GetComponent<Light>().color = Color.white;
            }
            else if (grabbedBy = null)
            {
                GetComponent<Renderer>().material = graph.materialStandard;
                GetComponent<Light>().color = Color.cyan;
            }

        }
    }


    public void CheckForBreak()
    {
        if(grabbedBy != null)
        {
            Node otherNode = null;
            if (graph.grabLeft == this && graph.grabRight != null)
                otherNode = graph.grabRight;
            else if(graph.grabRight == this && graph.grabLeft != null)
                otherNode = graph.grabLeft;

            if (connectedNodes.Contains(otherNode))
            {
                float currentDistance = Vector3.Distance(gameObject.transform.position, otherNode.gameObject.transform.position);

                Edge findEdge = attractionlist.Find(x => x.Other(this) == otherNode);

                float strain = (float)(currentDistance / (graph.initialGrabDistance * graph.ripFactor));
                findEdge.Strain(strain);

                //Debug.Log("Check for break " + name + otherNode.name + strain.ToString());

                if (currentDistance > graph.initialGrabDistance * graph.ripFactor)
                {
                    Debug.Log("BREAK!");
                    BreakConnection(otherNode, findEdge);

                    if (attractionlist.Count == 0)
                        Separate();

                }
            }
        }
    }

    public void Separate()
    {
        graph.nodes.Remove(this);
        graph.infectedNodes.Remove(this);

        if(graph.evilNodes.Count == 0)
        {
            graph.evilMaster = this;
            dontRepel = true;
        }

        foreach(Node n in graph.evilNodes)
        {
            Edge newEdge = Edge.CreateEdge(graph, n, this, 1f);

        }

        graph.evilNodes.Add(this);        

        //gameObject.AddComponent<Rigidbody>();
    }


    public void BreakConnection(Node otherNode, Edge connectingEdge)
    {
        otherNode.attractionlist.Remove(connectingEdge);
        attractionlist.Remove(connectingEdge);

        otherNode.connectedNodes.Remove(this);
        connectedNodes.Remove(otherNode);

        if (otherNode.endangered)
            otherNode.endangered = false;

        //Debug.Log("Destroyed " + connectingEdge.name);
        Destroy(connectingEdge.gameObject);
    }



    public void InfectOthers()
    {
        if(Time.time - timeOfInfection > graph.infection.incubation)
        {
            foreach(Edge e in attractionlist)
            {
               e.endangered = true;
               e.Other(this).endangered = true;
               e.Other(this).TryToInfect();
            }
        }
    }


	protected IEnumerator ChangeNodeColor (Material startMat, Material endMat)
	{
		changingColor = true;
		Debug.Log ("Lerping between " + startMat.name + " and " + endMat.name);
		for (float i = 0; i < 1; i = i + 0.01f) {
			GetComponent<Renderer> ().material.Lerp (startMat, endMat, i);
			GetComponent<Light> ().color = Color.Lerp (startMat.GetColor ("_RimColor"), endMat.GetColor ("_RimColor"), i);

			foreach (Edge e in attractionlist) {
				e.GetComponent<Renderer> ().material.Lerp (startMat, endMat, i);
			}

			yield return new WaitForEndOfFrame();
		}
		changingColor = false;
	}


	public void RefreshRepulsionList ()
	{
		repulsionlist.Clear ();
		GameObject[] allnodes = GameObject.FindGameObjectsWithTag ("Node");
		foreach (GameObject go in allnodes) {
			if (go != gameObject)
				repulsionlist.Add (go.GetComponent<Node> ());
		}
		calculate = true;
	}

    public void Start()
    {
        namefield.text = name;
        Reset();
        foreach (Edge e in attractionlist)
        {
            connectedNodes.Add(e.Other(this));
        }
    }

	public void Reset ()
	{
		if (graph)
			transform.localPosition = graph.center.transform.position + new Vector3 (Random.value - 0.5f, Random.value - 0.5f, Random.value - 0.5f);
	}
    



    public void Accelerate(Vector3 force)
    {
        throwVelocity = force;
    }

    public void CalculateEvilDrag()
    {
        masterVelocity = Vector3.zero;

        if(graph.infectedNodes.Count > 0)
        {
            Vector3 attractPosition = graph.infectedNodes.First().transform.position;
            masterVelocity += (attractPosition - transform.position).normalized * graph.masterSpeed;
           // Debug.Log("ATTRACT " + name + " to " + graph.infectedNodes.First().name + " with " + masterVelocity.ToString());
        }
    }


    protected void CalculateForces()
    {
        forceVelocity = Vector3.zero;

        // REPULSION
        foreach (Node rn in repulsionlist)
            forceVelocity += CalcRepulsion(rn);

        //ATTRACTION
        foreach (Edge e in attractionlist)
            forceVelocity += CalcAttraction(e.Other(this), e.weight);

        //ATTRACTION TO CENTER
        forceVelocity += CalcAttractionToCenter();
    }


    public void ApplyForces ()
	{
		if (!float.IsNaN (forceVelocity.x) && !float.IsNaN (forceVelocity.y) && !float.IsNaN (forceVelocity.z)) {
			
			transform.localPosition += forceVelocity * graph.damping * Time.deltaTime;

			transform.localPosition += throwVelocity * Time.deltaTime;
            transform.localPosition += masterVelocity * Time.deltaTime;

            savedPosition = transform.localPosition;

			throwVelocity = new Vector3 (throwVelocity.x * 0.8f, throwVelocity.y * 0.8f, throwVelocity.z * 0.8f);

		} else
			Debug.LogError (name + " " + forceVelocity.ToString ());
	}

	public void Update ()
	{		
		CheckGrab ();
        
        if(infected)
        { 
            InfectOthers();
            CheckForBreak();
        }

        if (calculate) {
			
			if (grabbedBy != null) {
				//Debug.Log (name + " grabbed by " + grabbedBy);
				transform.position = grabbedBy.transform.position;
			} else {
				CalculateForces ();

                if(graph.evilMaster == this)
                { 
                    CalculateEvilDrag();

                    if(graph.infectedNodes.Count > 0)
                    { 
                        if(Vector3.Distance(transform.position, graph.infectedNodes.First().transform.position) < graph.gameOverThreshold && !graph.gameOver)
                        {
                            graph.GameOver();
                        }
                    }

                }

                if(!graph.gameOver)
                { 
                    ApplyForces ();
                }
            }
		}

		CheckHighlight ();
	}

}
