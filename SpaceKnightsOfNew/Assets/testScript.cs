using UnityEngine;
using System.Collections;
using Awesomium.Unity;
using Awesomium.Mono;

public class testScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
		StartCoroutine(this.gameObject.GetComponent<WebViewTextureSample>().SafeLoad("http://reddit.com"));
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
