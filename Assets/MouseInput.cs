using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseInput : MonoBehaviour 
{

	Ray ray;
	RaycastHit hitInfo;

	DeformableMesh snow;

	public static MouseInput instance;

	void Awake()
	{
		instance = this;
	}

	void Update()
	{
		if(Input.GetMouseButton(0))
		{
			ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if(Physics.Raycast(ray, out hitInfo))
			{
				snow = hitInfo.collider.gameObject.GetComponent<DeformableMesh>();

				if(snow)
				{
					Vector3 inputPosition = hitInfo.point;
					inputPosition += hitInfo.normal * 0.1f;
					snow.AddForce(inputPosition);
				}
			}
		}
	}
}
