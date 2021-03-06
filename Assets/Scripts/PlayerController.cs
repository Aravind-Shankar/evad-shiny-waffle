﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour {

	/**
	*	Script for getting input from the user and controlling the player.
	*
	*	This script has to be attached as a component to the player to control.
	*	Also the player object must have a child empty GameObject called "ground_check"
	*	vertically below it, so that the jump mechanism can work properly.
	*	This empty child must be close enough to the player object so that if and only if the player is grounded,
	*	the ground_check object also touches/goes into the ground.
	*
	*	See "TestCharacter.unity" for an example scene with a ball as the player.
	*/

	public static int points = 0;
	private static int lives = Constants.START_LIVES;

	private bool canJump = false;
	private bool grounded = false;
	private bool facingRight = true;
	private bool gotTrophy = false;
	private Transform groundCheckLeft, groundCheckRight;
	private Transform radarPlayer;
    private int groundLayerMask;

	public int timeLeftMinutes, timeLeftSeconds;
	public float horizontalSpeed = 3.5f;
	public float jumpSpeed = 8f;
	public float respawnDelaySeconds = 1.0f;
	public string nextLevelName = "Level1";
    public TextMesh score;
	public TextMesh trophyMessageBox;
	public TextMesh lifeCountBox;
	public TextMesh timestampBox;
	public GameObject door;
	public Transform spawnPoint;

	void Start() {
		radarPlayer = transform.Find("RadarPlayer");
		groundCheckLeft = transform.Find ("Ground Check Left");
		groundCheckRight = transform.Find ("Ground Check Right");
        groundLayerMask = (1 << LayerMask.NameToLayer ("Ground Layer"));
        UpdateScore();
		UpdateLives ();
		if (timestampBox != null) {
			UpdateTimestamp();
			StartCoroutine(CountdownTimer());
		}
    }

	void Update() {
		grounded = Physics2D.Linecast(transform.position, groundCheckLeft.position, groundLayerMask) ||
			Physics2D.Linecast(transform.position, groundCheckRight.position, groundLayerMask);
		if (Input.GetButtonDown ("Jump") && grounded)
			canJump = true;
	}
	
	void FixedUpdate () {
		float horiz = Input.GetAxis ("Horizontal");
		Vector2 newVelocity = GetComponent<Rigidbody2D> ().velocity;
		newVelocity.x = (horiz == 0.0f) ? 0.0f : Mathf.Sign (horiz) * horizontalSpeed;
		if ((horiz < 0 && facingRight) || (horiz > 0 && !facingRight))
			Flip ();
		if (canJump) {
			newVelocity.y = jumpSpeed;
			canJump = false;
		}
		GetComponent<Rigidbody2D> ().velocity = newVelocity;
	}

    void OnTriggerEnter2D(Collider2D other)
    {
		GameObject otherObject = other.gameObject;
        if (otherObject.CompareTag ("Pick up")) {
			otherObject.SetActive (false);
			UpdateScore (Constants.POINTS_DUMMY_PICKUP);
		} else if (otherObject.CompareTag ("White Gem Pickup")) {
			otherObject.SetActive (false);
			UpdateScore (Constants.POINTS_WHITE_GEM);
		} else if (otherObject.CompareTag ("Red Gem Pickup")) {
			otherObject.SetActive (false);
			UpdateScore (Constants.POINTS_RED_GEM);
		} else if (otherObject.CompareTag ("Pink Ball Pickup")) {
			otherObject.SetActive (false);
			UpdateScore (Constants.POINTS_PINK_BALL);
        }
        else if (otherObject.CompareTag("Special Gem"))
        {
            otherObject.SetActive(false);
            UpdateScore(Constants.POINTS_SPECIAL);
        }
        else if (otherObject.CompareTag("Extra Life Pickup")) {
			otherObject.SetActive(false);
			++lives;
			UpdateLives();
			UpdateScore(Constants.POINTS_EXTRA_LIFE);
		} else if (otherObject.CompareTag("Wormhole Point")) {
			otherObject.GetComponentInParent<WormholeController>().EnterWormhole(otherObject);
		} else if (otherObject.CompareTag("Checkpoint")) {
			this.spawnPoint = otherObject.transform;
			otherObject.GetComponent<SpriteRenderer>().color = Color.green;
			otherObject.GetComponent<Collider2D>().enabled = false;
		} else if (otherObject.CompareTag ("Trophy")) {
			otherObject.SetActive(false);
			gotTrophy = true;
			trophyMessageBox.gameObject.SetActive(true);
			door.GetComponent<Renderer>().enabled = true;
			UpdateScore(Constants.POINTS_TROPHY);
		} else if (otherObject.CompareTag ("Door")) {
			if (gotTrophy) {
				UpdateScore(Constants.POINTS_DOOR);
				if (nextLevelName != null && !nextLevelName.Equals(""))
					Application.LoadLevel(nextLevelName);
				else Application.Quit();
			}
		}
        else if (otherObject.CompareTag("Gem fusion"))
        {
            int total = 0;
            var pos = otherObject.transform.position;
            var a=FindGameObjectsWithLayer(LayerMask.NameToLayer("Gems"));
            Vector3 p = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, Camera.main.nearClipPlane));

            for(int i=0;i<a.Length;i++)
            {
                if(((a[i].transform.position.x <= p.x)&&(pos.x <= p.x)) || ((a[i].transform.position.x >= p.x) && (pos.x >= p.x)))
                {
                    if (a[i].CompareTag("White Gem Pickup"))
                    {
                        a[i].SetActive(false);
                        total+=Constants.POINTS_WHITE_GEM;
                    }
                    else if (a[i].CompareTag("Red Gem Pickup"))
                    {
                        a[i].SetActive(false);
                        total+=Constants.POINTS_RED_GEM;
                    }
                    else if (a[i].CompareTag("Pink Ball Pickup"))
                    {
                        a[i].SetActive(false);
                        total+=Constants.POINTS_PINK_BALL;
                    }
                }

               otherObject.GetComponent<Renderer>().enabled = false;
               otherObject.transform.Find("Special Gem").gameObject.SetActive(true);
               otherObject.transform.Find("Special Gem").gameObject.GetComponent<Renderer>().enabled=true;
               otherObject.GetComponent<Collider2D>().enabled = false;
               Constants.POINTS_SPECIAL = total;
            }
        }
    }

    GameObject [] FindGameObjectsWithLayer(int layer)
    {
         var goArray = (GameObject[])FindObjectsOfType(typeof(GameObject));
         List<GameObject> list = new List<GameObject>();

         for (int i = 0; i<goArray.Length; i++)
         {
             if (goArray[i].layer == layer)
             {
                 list.Add(goArray[i]);
             }
         }
         if (list.Count == 0) {
             return null;
         }
         return list.ToArray();
   }

	void UpdateScore() {
		score.text = "Score: " + points.ToString();
	}

	void UpdateLives() {
		lifeCountBox.text = "x " + lives.ToString();
	}

	void UpdateTimestamp() {
		timestampBox.text = timeLeftMinutes.ToString() + ":" + timeLeftSeconds.ToString() + " left";
		if (timeLeftMinutes == 0)
			timestampBox.color = Color.yellow;
	}
	
	void UpdateScore(int gainedPoints) {
		points += gainedPoints;
		UpdateScore ();
	}

	void Flip() {
		Vector3 newScale = transform.localScale;
		newScale.x *= -1;
		transform.localScale = newScale;
		newScale = radarPlayer.localScale;
		newScale.x *= -1;
		radarPlayer.localScale = newScale;
		facingRight = !facingRight;
	}

	public void Initialize() {
		GetComponent<Rigidbody2D>().velocity = Vector2.zero;
		canJump = false;
		grounded = false;
		if (!facingRight)
			Flip ();
	}

	public void Disappear() {
		gameObject.GetComponent<Renderer> ().enabled = false;
		radarPlayer.gameObject.GetComponent<Renderer> ().enabled = false;
		gameObject.GetComponent<Collider2D> ().enabled = false;
	}

	public bool DieAndCheck() {
		Disappear ();
		if (lives > 0) {
			--lives;
			UpdateLives ();
			return (lives > 0);
		} else
			return false;
	}
	
	public IEnumerator Respawn(Transform spawnPoint) {
		yield return new WaitForSeconds(respawnDelaySeconds);
		radarPlayer.gameObject.GetComponent<Renderer> ().enabled = true;
		gameObject.GetComponent<Renderer> ().enabled = true;
		gameObject.GetComponent<Collider2D> ().enabled = true;
		transform.position = spawnPoint.transform.position;
		Initialize();
	}

	private IEnumerator CountdownTimer() {
		int totalSeconds = 60 * timeLeftMinutes + timeLeftSeconds;
		while (totalSeconds > 0) {
			yield return new WaitForSeconds(1.0f);
			--totalSeconds;
			timeLeftMinutes = totalSeconds / 60;
			timeLeftSeconds = totalSeconds % 60;
			UpdateTimestamp();
		}
		Application.Quit ();
	}
}
