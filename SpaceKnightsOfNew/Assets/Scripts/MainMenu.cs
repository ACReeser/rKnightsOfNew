using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Awesomium.Unity;

public class MainMenu : MonoBehaviour {
	public GameObject mainCam, spaceship, spawner, screenPrefab;
	private RedditAPI reddit;
	private float slideRightTime;
	public List<LinkBrief> currentLinks;
	private GameObject firstScreen, backScreen;
	private bool authenticated, submitting, hold=false;
	private WebViewTextureSample currentBrowser;
	private float wheelCache, browserSpeed=.5f;
	public VoteRack upvoter, downvoter;
	public Transform startTransform;
	private Vector3 startPosition;
	private Quaternion startRotation;
	private string subreddit="";
	
	public class LinkBrief{
		public Hashtable thing;
		public string title;
		public string url;
		public GameObject shard;
		public bool nsfw;
		public LinkBrief(Hashtable thing){
			this.thing = thing;
			title = thing["title"].ToString();
			url = thing["url"].ToString();
			nsfw = (bool)thing["over_18"];
		}
	}
	// Use this for initialization
	void Start () {
		spaceship.SetActiveRecursively(false);
		slideRightTime = spaceship.animation["slerp"].length;
		slideRightTime = spaceship.animation["slerp"].speed = .01f;
		spawner.active = false;
		reddit = new RedditAPI("SpaceKnightsOfNew_Game v.2 /u/publicstaticlloyd");
		
		currentLinks = new List<LinkBrief>();
		startPosition = startTransform.position;
		startRotation = startTransform.rotation;
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown(KeyCode.Escape) && !loggingIn){
			ToggleBrowserMove(false);
			hold = true;
			loggingIn = true;
			StartCoroutine(SpinToMainMenu());
		} else if (Input.GetMouseButton(0) && !loggingIn){
			TriggerUpvote();
		} else if (Input.GetMouseButton(1) && !loggingIn){
			TriggerDownvote();
		}
		if (Input.GetKeyDown(KeyCode.Space)){
			hold = true;
			ToggleBrowserMove(false);
		} else if (Input.GetKeyUp(KeyCode.Space)){
			hold = false;
			ToggleBrowserMove(true);
		}
		if(!hold){
			wheelCache = Input.GetAxis("Mouse ScrollWheel");
			ChangeBrowserSpeed(wheelCache/10);
		}
		spaceship.animation["slerp"].normalizedTime = (Input.mousePosition.x/Screen.width);
	}
	
	private void ToggleBrowserMove(bool goForward){
		if(currentBrowser != null){
			if (goForward){
				currentBrowser.animation["screenForward"].speed = browserSpeed;
			} else {
				currentBrowser.animation["screenForward"].speed = 0f;
			}
		}
	}
	
	private void ChangeBrowserSpeed(float modifier){
		if(currentBrowser != null){
			currentBrowser.gameObject.animation["screenForward"].speed += modifier;
			browserSpeed = currentBrowser.gameObject.animation["screenForward"].speed;
		}
	}
	
	private void TriggerUpvote(){
		spaceship.transform.Find("upvote").GetComponent<VoteRack>().FireVote();
		if (authenticated){
			print ("trying to upvote something");
		}
	}
	
	private void TriggerDownvote(){
		spaceship.transform.Find("downvote").GetComponent<VoteRack>().FireVote();
		if (authenticated){
			print ("trying to downvote something");
		}
	}
	
	private string username = "";
	private string password = "";
	private string error = "";
	private bool loggingIn = true, offline = true; 
	private float counter;
	private static Quaternion mainMenu = Quaternion.Euler(Vector3.up*-180);
	private static Quaternion game = Quaternion.Euler(Vector3.zero);
	
	void OnGUI(){
		if (loggingIn){
			GUI.BeginGroup (new Rect (Screen.width / 2 - 150, Screen.height / 2 - 250, 300, 500));
			GUI.Box (new Rect (0,0,300,50), "Login to reddit");
			GUI.Label (new Rect (0, 50, 300, 50), error);
			GUI.Label (new Rect (0, 100, 300, 100), "Username");
			username = GUI.TextField (new Rect (0, 130, 300, 30), username);
			if (!submitting){
				GUI.Label (new Rect (0, 160, 300, 30), "Password");
				password = GUI.TextField (new Rect (0, 190, 300, 30), password);
			} else {
				GUI.Label(new Rect(0, 160, 300, 30), "Logging in...");
			}
			if (!authenticated){
				if (GUI.Button (new Rect (0,220,300,30), "Submit")){
					TestLogin();
				}
			}
			if (!authenticated){
				if (GUI.Button (new Rect (0,250,300,30), "Play Anonymously")){
					BeginGame();
				}
			} else {
				GUI.Label(new Rect(0, 250, 300, 30), "Subreddit");
				subreddit = GUI.TextField (new Rect (0, 280, 300, 30), subreddit);
				if (GUI.Button (new Rect (0,310,300,30), "Play!")){
					BeginGame();
				}
			}
			if (GUI.Button (new Rect (0,400,300,20), "Exit")){
				Application.Quit();
			}
			GUI.EndGroup();
		}
	}
	public void RegisterDownboat(){
		downvoter.ExpendVote();
	}
	public void RegisterUpboat(){
		upvoter.ExpendVote();
	}
	
	private void TestLogin(){
		try {
			submitting = true;
			if (reddit.Login(username, password)){
				authenticated = true;
				print ("You're logged in!");
				error = "Logged in!";
			}
			submitting = false;
		} catch (InvalidUserPass e) {
			Debug.LogWarning("Didn't log in correctly! "+e.Message);
			error = "Login failed";
		}
	}
	
	private void BeginGame(){
		loggingIn = false;
		StartCoroutine(SpinToGame());
		StartCoroutine(printPage());
	}
	
	private void PrintHash(Hashtable h, string prefix){
		foreach(var s in h.Keys){
			Debug.Log(prefix+" ["+s.ToString()+"]" + " "+h[s].ToString());
		}
	}
	
	private IEnumerator printPage(){
		Hashtable r = reddit.GetFrontPage();
		if (r != null){
			foreach(int i in r.Keys){
				Hashtable thing = (r[i] as Hashtable);
				currentLinks.Add(new MainMenu.LinkBrief(thing["data"] as Hashtable));
				yield return new WaitForSeconds(.01f);
			}
			StartCoroutine(FeedLinks());
		}
	}
	
	private void LoadUpBrowser(GameObject theG, LinkBrief l){
		WebViewTextureSample theBrowser =  theG.GetComponent<WebViewTextureSample>();
		theBrowser.SetInitialURL(l.url);
		theBrowser.SafeLoad(l.url);
		theBrowser.SetTitle(l.title);
		theBrowser.SetScore(int.Parse(l.thing["score"].ToString())); 
	}
	
	private void StartScreenForward(WebViewTextureSample browser){
		browser.MakeVisible();
		browser.gameObject.animation.enabled = true;
		browser.gameObject.animation["screenForward"].speed = browserSpeed;
		browser.gameObject.animation.Play();
	}
	
//	private void StopBrowser(WebViewTextureSample browser){
//		browser.gameObject.renderer.enabled = false;
//		browser.gameObject.collider.enabled = false;
//		browser.moveSpeed = browserSpeed;
//	}
	
	private IEnumerator FeedLinks(){
		bool movingFirst = true;
		LinkBrief nextLink;
		firstScreen = (GameObject)GameObject.Instantiate(screenPrefab, startPosition, startRotation);
		LoadUpBrowser(firstScreen, currentLinks[0]);
		
		for(int i = 1; i < currentLinks.Count-1; i++){
			nextLink = currentLinks[i];
			if (nextLink.nsfw){
				i++;
				if (i >= currentLinks.Count){break;}
				nextLink = currentLinks[i];
			}
			if (movingFirst){
				backScreen = (GameObject)GameObject.Instantiate(screenPrefab, startPosition, startRotation);
				LoadUpBrowser(backScreen, nextLink);
				currentBrowser = firstScreen.GetComponent<WebViewTextureSample>();
			} else {
				firstScreen = (GameObject)GameObject.Instantiate(screenPrefab, startPosition, startRotation);
				LoadUpBrowser(firstScreen, nextLink);
				currentBrowser = backScreen.GetComponent<WebViewTextureSample>();
			}
			StartScreenForward(currentBrowser);
			while(currentBrowser.animation.isPlaying){
				yield return new WaitForSeconds(Time.deltaTime);
			}
//			StopBrowser(currentBrowser);
//			currentBrowser.gameObject.transform.position = startPosition;
//			currentBrowser.gameObject.transform.rotation = startRotation;
			Destroy(currentBrowser);
			movingFirst = !movingFirst;
		}
		//out of links!
	}
	
	private IEnumerator SpinToGame(){
		counter = 0f;
		while(counter < .75f){
			mainCam.transform.localRotation = Quaternion.Slerp(mainMenu, game, counter);
			counter += Time.deltaTime;
			yield return new WaitForSeconds(Time.deltaTime);
		}
		counter = 0f;
		mainCam.transform.rotation = game;
		spaceship.SetActiveRecursively(true);
		spawner.active = true;
	}
	
	private IEnumerator SpinToMainMenu(){
		counter = 0f;
		while(counter < .75f){
			mainCam.transform.localRotation = Quaternion.Slerp(game, mainMenu, counter);
			counter += Time.deltaTime;
			yield return new WaitForSeconds(Time.deltaTime);
		}
		counter = 0f;
		mainCam.transform.rotation = mainMenu;
		spaceship.SetActiveRecursively(false);
		spawner.active = false;
	}
}
