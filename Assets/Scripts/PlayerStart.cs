using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[RequireComponent(typeof(NetworkStartPosition))]

public class PlayerStart : MonoBehaviour
{

	/// <summary>
    /// Start - Hide the mesh renderer. We only want this
    /// to show up in editor
    /// </summary>
	void Start ()
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        renderer.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
