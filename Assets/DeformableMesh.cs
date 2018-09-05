using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

[RequireComponent(typeof(MeshFilter),(typeof(MeshRenderer)))]
public class DeformableMesh : MonoBehaviour 
{
	[Header("Size Settings:")]
	[SerializeField] float verticalSize;
	[SerializeField] float horizontalSize;

	[Header("Material:")]
	[SerializeField] Material meshMaterial;

	[Header("Indentation Settings:")]
	[SerializeField] float force;
	[SerializeField] float radius;

	Mesh mesh;
	MeshFilter meshFilter;
	MeshRenderer meshRenderer;
	MeshCollider meshCollider;

	//MeshInformation
	Vector3[] vertices;
	Vector3[] modifiedVertices;
	int[] triangles;

	Vector2 verticeAmount;
	
	List<HandledResult> scheduledJobs = new List<HandledResult>();

	void Awake() 
	{
		meshRenderer = GetComponent<MeshRenderer>();
		meshFilter = GetComponent<MeshFilter>();
		meshFilter.mesh = new Mesh();
		mesh = meshFilter.mesh;

		GeneratePlane();	

	}

	void Update() 
	{
		if(scheduledJobs.Count > 0)
		{
			for(int i = 0; i < scheduledJobs.Count; i++)
			{
				CompleteJob(scheduledJobs[i]);
			}
		}
	}

	/*Now a Mesh is build out of vertices and triangles there are basically build up out of three
	of its vertices - we will start by working out the positions of our vertices.
	Our vertices need an array of Vector3 as they have 3D positions in our world. The length of said array is dependend
	on the size of our generated plane. It's easiest to imagine our plane with a grid overlay on top, each of our grids fields needs a
	vertice at each of it's corners but adjacent fields can obviously share corners - knowing that we now know the we are going to need one more 
	vertice than we have fields in each dimensions */
	void GeneratePlane()
	{
		vertices = new Vector3[((int)horizontalSize + 1) * 
		((int)verticalSize + 1)];
		Vector2[] uv = new Vector2[vertices.Length];

		/*Let's position our vertices accordingly unsing a nested for loop*/
		for(int z = 0, y = 0; y <= (int)verticalSize; y++)
		{
			for(int x = 0; x <= (int)horizontalSize; x++, z++)
			{
				vertices[z] = new Vector3(x,0,y);
				uv[z] = new Vector2(x/(int)horizontalSize,
				y/(int)verticalSize);
			}
		}

		/*Now that we have generated and position our vertices we should take a look at generating a proper mesh out of this.
		We'll start this process by setting our vertices as the mesh vertices */
		mesh.vertices = vertices;

		/*We also need to make sure that our vertices and modified verticies match up 
		at the very beginning */
		modifiedVertices = new Vector3[vertices.Length];
		for(int i = 0; i < vertices.Length; i++)
		{
			modifiedVertices[i] = vertices[i];
		}

		mesh.uv = uv;

		/*The mesh shouldn't show up yet as it doesnt have any triangles at that point. We'll generate these by looping over the points thaty build 
		up our triangles, their indiced will go into our array of int's called triangles */
		triangles = new int[(int)horizontalSize * 
		(int)verticalSize * 6];

		for(int t = 0, v = 0, y = 0; y < (int)verticalSize; y++, v++)
		{
			for(int x = 0; x <(int)horizontalSize; x++, t+= 6, v++)
			{
				triangles[t] = v;
				triangles[t + 3] = triangles[t + 2] = v + 1; 
				triangles[t + 4] = triangles[t + 1] = v + (int)horizontalSize + 1;
				triangles[t + 5] = v + (int)horizontalSize + 2;
			}
		}

		/*Finally we need to assign our triangles as the mesh triangles and recalculate the normals to ensure the lighting will be correct.*/
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		mesh.RecalculateTangents();

		/*We will also need a collider, this is necessary so we are able to use the physics syste to detect interactions */
		meshCollider = gameObject.AddComponent<MeshCollider>();
		meshCollider.sharedMesh = mesh;


		//We also need to set our MeshMaterial to avoid seeing an ugly magenta colored plane!
		meshRenderer.material = meshMaterial;
	}

	void OnCollisionEnter(Collision other) {
		if(other.contacts.Length > 0)
    	{
     		Vector3[] contactPoints = new Vector3[other.contacts.Length];
      		for(int i = 0; i < other.contacts.Length; i++)
      		{
        		Vector3 currentContactpoint = other.contacts[i].point;
				currentContactpoint = transform.InverseTransformPoint(currentContactpoint);
        		contactPoints[i] = currentContactpoint;
      		}

			HandledResult newHandledResult = new HandledResult();
			IndentSnow(force,contactPoints,ref newHandledResult);
     
    	}
	}

	public void AddForce(Vector3 inputPoint)
	{
		StartCoroutine(MarkHitpointDebug(inputPoint));

	}

	

	IEnumerator MarkHitpointDebug(Vector3 point)
	{
		GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		marker.AddComponent<SphereCollider>();
		marker.AddComponent<Rigidbody>();
		marker.transform.position = point;
		yield return new WaitForSeconds(0.5f);
		Destroy(marker);

	}

	void IndentSnow(float force, Vector3[] worldPositions,ref HandledResult newHandledResult)
	{

		newHandledResult.contactpoints = new NativeArray<Vector3>
		(worldPositions, Allocator.TempJob);
		newHandledResult.initialVerts = new NativeArray<Vector3>
 	 	(vertices, Allocator.TempJob);
		newHandledResult.modifiedVerts = new NativeArray<Vector3>
 		(modifiedVertices, Allocator.TempJob);
  
  		IndentationJob meshIndentationJob = new IndentationJob
 		{
			 contactPoints = newHandledResult.contactpoints,
			 initialVertices = newHandledResult.initialVerts,
			 modifiedVertices = newHandledResult.modifiedVerts,
			 force = force,
			 radius = radius
  		};

  		JobHandle indentationJobhandle = meshIndentationJob.Schedule(newHandledResult.initialVerts.Length,newHandledResult.initialVerts.Length);
  		
		newHandledResult.jobHandle = indentationJobhandle;

		scheduledJobs.Add(newHandledResult);
	}

	void CompleteJob(HandledResult handle)
	{
		scheduledJobs.Remove(handle);

		handle.jobHandle.Complete();
  
		handle.contactpoints.Dispose();
		handle.initialVerts.Dispose();
		handle.modifiedVerts.CopyTo(modifiedVertices);
		handle.modifiedVerts.Dispose();

		mesh.vertices = modifiedVertices;
		vertices = mesh.vertices;
		mesh.RecalculateNormals();
			
	}
}

struct HandledResult
{
	public JobHandle jobHandle;
	public NativeArray<Vector3> contactpoints;
	public NativeArray<Vector3> initialVerts;
  	public NativeArray<Vector3> modifiedVerts;
}
