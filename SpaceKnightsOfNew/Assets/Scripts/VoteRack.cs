using UnityEngine;
using System.Collections;

public class VoteRack : MonoBehaviour {
	public bool isUpvote;
	private bool firing;
	public GameObject myVote, upvoteExplosion, downvoteExplosion;
	private float fireCount=0f;
	public float maxFireTime = 4f;
	public float beginSpeed = .01f;
	private float speed;
	public float acceleration = .01f;
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (firing){
			if (fireCount > maxFireTime){
				myVote.transform.tag = "noboat";
				myVote.transform.Find("Cylinder").Find("smokeExhaust").particleSystem.Stop();
				myVote.transform.Find("Cylinder").Find("fireExhaust").particleSystem.Stop();
				myVote.transform.parent = this.transform;
				myVote.transform.localPosition = Vector3.zero;
				fireCount = 0f;
				firing = false;
				speed = beginSpeed;
			} else {
				myVote.transform.Translate(Vector3.up*speed, Space.Self);
				fireCount+=Time.deltaTime;
			}
		}
	}
	
	void LateUpdate(){
		if (firing){
			speed = speed+(Time.deltaTime*acceleration);
		}
	}
	
	public void FireVote(){
		if (firing == false){
			speed = beginSpeed;
			myVote.transform.Find("Cylinder").Find("smokeExhaust").particleSystem.Play();
			myVote.transform.Find("Cylinder").Find("fireExhaust").particleSystem.Play();
			if (isUpvote){
				myVote.transform.tag = "upboat";
			} else {
				myVote.transform.tag = "downboat";
			}
			myVote.transform.parent = null;
			firing = true;
		}
	}
	
	public IEnumerator CeaseExplosion(ParticleSystem ps){
		yield return new WaitForSeconds(1.3f);
		ps.Stop();
	}
	
	public void ExpendVote(){
		if (isUpvote){
			upvoteExplosion.transform.position = myVote.transform.position+Vector3.back;
			upvoteExplosion.transform.particleSystem.Play();
			StartCoroutine(CeaseExplosion(upvoteExplosion.transform.particleSystem));
		} else {
			downvoteExplosion.transform.position = myVote.transform.position+Vector3.back;
			downvoteExplosion.transform.particleSystem.Play();
			StartCoroutine(CeaseExplosion(downvoteExplosion.transform.particleSystem));
		}
		fireCount = maxFireTime +1f;
	}
	
}
