using Awesomium.Mono;
using Awesomium.Unity;
using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Script that allows handling of event of a WebTexture component.
/// Add this script to the same game object you added a WebTexture.
/// </summary>
public class WebTextureHandler : MonoBehaviour {
	
	private WebTexture webTexture;
	// Use this for initialization
	void Awake () 
	{
		// Get the WebTexture component of this game object.
		webTexture = this.GetComponent<WebTexture>();
		
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}
	
	public void SetURL(string title, string url){
		if (webTexture.IsLoadingPage){
			StartCoroutine(DelayedLoad(url));
		} else {
			this.renderer.enabled = true;
			webTexture.LoadURL(url);
		}
//		titleMesh.text = title;
	}
	
	private IEnumerator DelayedLoad(string url){
		while(webTexture.IsLoadingPage){
			yield return new WaitForSeconds(.2f);
		}
		this.renderer.enabled = true;
		webTexture.LoadURL(url);
	}
}
