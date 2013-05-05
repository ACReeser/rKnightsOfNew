/********************************************************************************
 *    Project   : Awesomium.NET (Awesomium.Unity)
 *    File      : WebViewTextureSample.cs
 *    Version   : 1.6.5.0
 *    Date      : 02/20/2012
 *    Author    : Perikles C. Stephanidis (perikles@stephanidis.net)
 *    Copyright : ©2012 Khrona LLC
 *-------------------------------------------------------------------------------
 *
 *    Notes     :
 *
 *    A simple script demonstrating how to wrap a WebView and display its 
 *    pixel buffer in a custom component. This script can be used as the basis 
 *    for user contributions and feedback. The predefined WebTexture uses a 
 *    similar approach to handle user input. If the behavior of WebTexture is not
 *    satisfactory, you can use and expand this script to provide your own 
 *    wrapping logic. Please provide any feedback in our Support Forums.
 *    
 *    Support   :
 * 
 *    http://support.awesomium.com
 *    
 *    API Docs  : 
 *    
 *    http://awesomium.com/docs/1_6_4/sharp_api/
 *    
 ********************************************************************************/

#region Using
using System;
using UnityEngine;
using Awesomium.Mono;
using Awesomium.Unity;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
#endregion


public class WebViewTextureSample : MonoBehaviour
{
    #region Fields
    // Public Variables
    [SerializeField()]
    private int width = 512;
    [SerializeField()]
    private int height = 512;
    [SerializeField()]
    private string initialURL = "http://google.com";
	public TextMesh titleMesh;
	public float moveSpeed = 5f;
	private bool acceptingInput = false;
	private int vote = 0;
	private int score;
	public Material upboatMat;
	public Material downboatMat;
	public GameObject votePlane;
	public TextMesh scoreMesh;
	public MainMenu mainScript;
	bool throttle = false;

    // Internal Variables
    private bool isFocused = false;
    private bool isScrollable = false;
    private WebView webView;
    private Texture2D texture;
    private Color32[] Pixels;
    private GCHandle PixelsHandle;
    #endregion


    #region Methods
    private bool CheckWebView()
    {
        return WebCore.IsRunning && ( webView != null ) && webView.IsEnabled;
    }
    #endregion

    #region Overrides
    public void Start()
    {
        Debug.Log( "Initializing WebCore." );

        // WebCoreInitializer.Awake() initializes the WebCore
        // before any Start() function on any script is called.
        // We create a web-view here.
        webView = WebCore.CreateWebView( width, height );

        // Load the defined URL.
        webView.LoadURL( initialURL );

        // Prepare and a assign a texture to the component.
        // The texture will display the pixel buffer of the WebView.
        texture = new Texture2D( width, height, TextureFormat.RGBA32, false );
        Pixels = texture.GetPixels32( 0 );
        PixelsHandle = GCHandle.Alloc( Pixels, GCHandleType.Pinned );

        if ( renderer )
            renderer.material.mainTexture = texture;
        else if ( GetComponent( typeof( GUITexture ) ) )
        {
            GUITexture gui = GetComponent( typeof( GUITexture ) ) as GUITexture;
            gui.texture = texture;
        }
        else
            Debug.LogError( "Game Object has no Material or GUI Texture, we cannot render a web-page to this object!" );
		
		mainScript = GameObject.Find("Main").GetComponent<MainMenu>();
        // Handle some important events.
        webView.OpenExternalLink += OnWebViewOpenExternalLink;
        webView.ShowJavascriptDialog += OnJavascriptDialog;
        webView.LoginRequest += OnLoginRequest;
    }

    private void OnEnable()
    {
        if ( !CheckWebView() )
            return;

        webView.IsRendering = true;
    }

    private void OnDisable()
    {
        if ( !CheckWebView() )
            return;

        webView.IsRendering = false;
    }

    public void Focus()
    {
        if ( !CheckWebView() )
            return;

        webView.Focus();
        isFocused = true;
    }

    public void Unfocus()
    {
        if ( !CheckWebView() )
            return;

        webView.Unfocus();
        isFocused = false;
    }

    private void Update()
    {
        if ( !CheckWebView() )
            return;

        if ( webView.IsDirty )
        {
			if (throttle){
				throttle = !throttle;
				return;
			}
			throttle = !throttle;
            Awesomium.Mono.RenderBuffer rBuffer = webView.Render();

            if ( rBuffer != null )
                Utilities.DrawBuffer( rBuffer, ref texture, ref Pixels, ref PixelsHandle );
        }
    }

    private void OnDestroy()
    {
        if ( CheckWebView() )
        {
            // Free the pinned array handle.
            PixelsHandle.Free();

            if ( WebCore.IsRunning )
            {
                webView.Close();
                webView = null;

                Debug.Log( "Destroyed View" );
            }
        }
    }

    private void OnApplicationQuit()
    {
        if ( CheckWebView() )
            Destroy( this );
    }
    #endregion

    #region Input Processing

    #region OnGUI
    private bool HandleEvent( Event e, out RaycastHit hit )
    {
        if ( !acceptingInput ){
			hit = new RaycastHit();
            return false;
		}
        if ( e.isMouse )
        {
            // We only inject mouse input that occurred in this GameObject.
            if ( GetGameObject( out hit ) != this.gameObject )
            {
                if ( e.type == EventType.MouseUp )
                    webView.InjectMouseUp( Utilities.GetMouseButton() );

                Unfocus();
                return false;
            }
            else
                return true;
        }
        else
        {
            hit = new RaycastHit();
            return ( e.isKey && isFocused ) || ( ( e.type == EventType.ScrollWheel ) && isScrollable );
        }
    }

    private void DOnTGUI()
    {
        if ( (!acceptingInput) || (!CheckWebView()) )
            return;

        Event e = Event.current;
        RaycastHit hit;

        if ( !HandleEvent( e, out hit ) )
            return;

        int x;
        int y;

        switch ( e.type )
        {

            case EventType.KeyDown:
            case EventType.KeyUp:
                webView.InjectKeyboardEvent( e.GetKeyboardEvent() );
                break;

            case EventType.MouseDown:
                Focus();
                x = (int)( hit.textureCoord.x * width );
                y = (int)( hit.textureCoord.y * height );
                webView.InjectMouseMove( x, height - y );
                webView.InjectMouseDown( Utilities.GetMouseButton() );
                break;

            case EventType.MouseUp:
                x = (int)( hit.textureCoord.x * width );
                y = (int)( hit.textureCoord.y * height );
                webView.InjectMouseMove( x, height - y );
                webView.InjectMouseUp( Utilities.GetMouseButton() );
                break;

            case EventType.ScrollWheel:
                webView.InjectMouseWheel( (int)e.delta.y * -10, 0 );
                break;

        }
    }

    static GameObject GetGameObject( out RaycastHit hit )
    {
        // Builds a ray from camera point of view to the mouse position.
        Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
        // Casts the ray and get the first game object hit.
        return Physics.Raycast( ray, out hit ) ? hit.transform.gameObject : null;
    }
    #endregion

    #region Mouse
    private void DOnTMouseOver()
    {
        if ( (!acceptingInput) || (!CheckWebView()) )
            return;

        RaycastHit hit;

        // Used for injecting a MouseMove event on a game object.
        if ( Physics.Raycast( Camera.main.ScreenPointToRay( Input.mousePosition ), out hit ) )
        {
            int x = (int)( hit.textureCoord.x * width );
            int y = (int)( hit.textureCoord.y * height );
            webView.InjectMouseMove( x, height - y );
        }
        else // Used for injecting a MouseMove event on a GUITexture.
        {
            GUITexture gui = GetComponent( typeof( GUITexture ) ) as GUITexture;
            if ( gui != null )
            {
                int x = (int)( ( Input.mousePosition.x ) - ( gui.pixelInset.x + Screen.width * transform.position.x ) );
                int y = (int)( ( Input.mousePosition.y ) - ( gui.pixelInset.y + Screen.height * transform.position.y ) );
                webView.InjectMouseMove( x, height - y );
            }
        }
    }

    private void DOnTMouseDown()
    {
        if ( (!acceptingInput) || (!CheckWebView()) )
            return;

        Focus();

        // Used for injecting a MouseDown event on a GUITexture
        GUITexture gui = GetComponent( typeof( GUITexture ) ) as GUITexture;

        if ( gui != null )
        {
            int x = (int)( ( Input.mousePosition.x ) - ( gui.pixelInset.x + Screen.width * transform.position.x ) );
            int y = (int)( ( Input.mousePosition.y ) - ( gui.pixelInset.y + Screen.height * transform.position.y ) );
            webView.InjectMouseMove( x, height - y );
            webView.InjectMouseDown( MouseButton.Left );
            Debug.Log( "Texture MouseDown" );
        }
    }

    private void DOnTMouseUp()
    {
        if ( (!acceptingInput) || (!CheckWebView()) )
            return;

        // Used for injecting a MouseUp event on a GUITexture
        GUITexture gui = GetComponent( typeof( GUITexture ) ) as GUITexture;
        if ( gui != null )
        {
            int x = (int)( ( Input.mousePosition.x ) - ( gui.pixelInset.x + Screen.width * transform.position.x ) );
            int y = (int)( ( Input.mousePosition.y ) - ( gui.pixelInset.y + Screen.height * transform.position.y ) );
            webView.InjectMouseMove( x, height - y );
            webView.InjectMouseUp( MouseButton.Left );
            Debug.Log( "Texture MouseUp" );
        }
    }

    private void DOnTMouseEnter()
    {
        if ( !acceptingInput )
            return;
        isScrollable = true;
    }

    private void DOnTMouseExit()
    {
        if ( !acceptingInput )
            return;
        isScrollable = false;

        if ( webView.IsEnabled && !Input.GetMouseButtonDown( 0 ) )
            webView.InjectMouseMove( -1, -1 );
    }
    #endregion

    #endregion
	
	public void SetInitialURL(string url){
		initialURL = (url);
	}
	
	public void SetTitle(string title){
		titleMesh.text = title;
	}
	
	public IEnumerator SafeLoad(string url){
		float counter = 0f;
		while (webView == null){
			counter += Time.deltaTime;
			yield return new WaitForSeconds(Time.deltaTime);
			if (counter > 10f){
				return true;
			}
		}
		webView.LoadURL(url);
	}
	
	void OnTriggerEnter(Collider other){
		if (other.gameObject.tag == "upboat"){
			Upboat();
			mainScript.RegisterUpboat();
		} else if (other.gameObject.tag == "downboat"){
			Downboat();
			mainScript.RegisterDownboat();
		}
	}
	
	private void Upboat(){
		if (vote == -1){
			score++;
			scoreMesh.text = score.ToString();
			votePlane.renderer.enabled = false;
			vote = 0;
		} else if (vote == 0){
			votePlane.renderer.material = upboatMat;
			votePlane.renderer.enabled = true;
			vote = 1;
			score++;
			scoreMesh.text = score.ToString();
		}
	}
	
	private void Downboat(){
		if (vote == 1){
			score--;
			scoreMesh.text = score.ToString();
			vote = 0;
			votePlane.renderer.enabled = false;
		} else if (vote == 0){
			votePlane.renderer.material = downboatMat;
			votePlane.renderer.enabled = true;
			vote = -1;
			score--;
			scoreMesh.text = score.ToString();
		}
	}
	
	public void MakeVisible(){
		this.gameObject.renderer.enabled = true;
		this.gameObject.collider.enabled = true;
		titleMesh.renderer.enabled = true;
		scoreMesh.renderer.enabled = true;
	}
	
	public void SetScore(int s){
		score = s;
		scoreMesh.text = s.ToString();
	}
	
    #region Event Handlers
    private void OnLoginRequest( object sender, LoginRequestEventArgs e )
    {
        if ( !CheckWebView() )
            return;

        // Ask user for credentials or provide them yourself.
        // Do not forget to set Cancel to false.
        e.Cancel = true;

        // Prevent further processing by the WebView.
        e.Handled = true;
    }

    private void OnJavascriptDialog( object sender, JavascriptDialogEventArgs e )
    {
        if ( !CheckWebView() )
            return;

        // Choose how to display a JS dialog.
        // Do not forget to set Cancel to false.
        e.Cancel = true;

        // Prevent further processing by the WebView.
        e.Handled = true;
    }

    private void OnWebViewOpenExternalLink( object sender, OpenExternalLinkEventArgs e )
    {
        if ( !CheckWebView() )
            return;

        // For this sample, we load the URL
        // in the same WebView.
        webView.LoadURL( e.Url );
    }
    #endregion
}