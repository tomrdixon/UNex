﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {
	public Image winImage;
	public Text winText;
	public Text playerScore;
	public Text playerHealthText;
	public Text playerWeaponText;
	public Text playerAmmoCountText;
	public Text playerEquipmentText;
	public Text playerEquipmentLeftText;
	public Text playerTimer;
	public GameObject pauseUI;
	public GameObject spectator;
	public NetworkManager networkManager;
	public int enemyStaticsDead =0;//initialising so that game doesn't end immediately - setting this in start may be an easier way to stop games ending when loading new level

	public static int enemStaticTotal=1;
	static short level;

	private float restartDelay = 3f;
	private float restartTime;
	private float timerSec=1f;
	private Color imagecolour = new Color(1f,0f,0f,.2f);
	private Color textcolour = new Color(0f,0f,0f,.5f);
	private Color zero;
	private GameObject player;
	private Health playerHealth;
	private WeaponManager playerWeapon;
	private EquipmentManager playerEquipment;
	private Shoot weaponAmmo;
	private UIManager uiManager;
	private short timerMin=0;

	public UIManager ReturnPauseUI()
	{
		return uiManager;
	}
	public GameObject ReturnSpectator()
	{
		return spectator;
	}
	
	void Start()
	{
		zero = new Color (0f,0f,0f,0f);
		level = (short)Application.loadedLevel;
		uiManager = gameObject.GetComponent<UIManager> ();
		//enemyStaticsDead = (GameObject.FindGameObjectsWithTag ("EnemyStatic").Length + GameObject.FindGameObjectsWithTag ("EnemyAlive").Length);
	}
	
	// Update is called once per frame
	void Update () {
//		if (Spawning.spawnedEnemies) { //poor way to get current amount of alive
//			enemyStaticsDead = (GameObject.FindGameObjectsWithTag ("EnemyStatic").Length);
//			Spawning.spawnedEnemies = false;}

		playerScore.text = (PlayerScore.enemiesTotal-enemyStaticsDead).ToString();

		if (!player) {
			player = GameObject.FindGameObjectWithTag ("Player");
		}
		else{
			HealthCount ();
			WeaponSelection ();
			EquipmentSelection();
			AmmoCount();
			KeepTime();
			if((Input.GetKeyDown(KeyCode.Escape) || UIManager.resume) && player && player.GetComponent<Health>().GetHealth() >0){
				//Debug.Log("pausegame: "+uiManager.resume);
				UIManager.resume = false;//these were made static - there will only ever be one instance per player
				uiManager.PausedResume (player);//abstracting the pause to the pause script
			}
			if(Input.GetKeyDown(KeyCode.Tab) && !uiManager.paused){
				uiManager.showHideSB();}
		}

		if (enemStaticTotal < 1) {
			LoadLevel ();	
		}
	}
	

	void GamePause()
	{
		//Debug.Log ("pressed"+ paused);
		uiManager.PausedResume (player);

	}
	
	void HealthCount()
	{
		if(!playerHealth){
			playerHealth = player.GetComponent<Health> ();
		}
		else{
			playerHealthText.text = "Health: "+ (int)playerHealth.GetHealth();
		}
	}

	void WeaponSelection()
	{
		if(!playerWeapon){
			playerWeapon = player.GetComponentInChildren<WeaponManager>();
		}
		else if(playerWeaponText){
			playerWeaponText.text = playerWeapon.currentWeapon.GetWeaponName();
		}
		else{
			Debug.LogError("playerWeaponText can't be found (it seems), for some reason");
		}
	 }
	void EquipmentSelection()
	{
		if(!playerEquipment){
			playerEquipment = player.GetComponentInChildren<EquipmentManager>();
		}
		else{
			playerEquipmentText.text = playerEquipment.GetEquipmentName();
			
		}
	}

	void AmmoCount()
	{
		if (!weaponAmmo) {
			weaponAmmo = player.GetComponentInChildren<Shoot>();		
		}
		else if(weaponAmmo.reloading){
			playerAmmoCountText.text = "Reloading";
		}
		else{
			playerAmmoCountText.text = weaponAmmo.GetCurrentAmmo().ToString();
		}

	}
	void KeepTime()
	{
		if(enemStaticTotal >0){//ensuring that the timer stops once enemies have been killed
			timerSec += Time.deltaTime;
			if(timerSec>=60f){
				timerSec = 0f;
				timerMin++;
			}
			playerTimer.text = timerMin.ToString ("00") + ":" + timerSec.ToString ("00");
		}

	}

	void LoadLevel()
	{
		Debug.Log ("Loading level");
		GameObject[] allGOs = GameObject.FindObjectsOfType<GameObject> ();
		winImage.color = Color.Lerp (winImage.color, imagecolour, 1.5f * Time.deltaTime);
		winText.color = Color.Lerp (winText.color, textcolour, 1f * Time.deltaTime);
		//Debug.Log(winImage.color = Color.Lerp(winImage.color, imagecolour ,1.5f * Time.deltaTime));
		//Debug.Log(winText.color = Color.Lerp(winText.color, textcolour ,1f * Time.deltaTime));
		restartTime += Time.deltaTime;
		Spawning.spawnedEnemies = false;//resetting spawnedEnemies for next level
		EnemyAI.alerted = false;// resetting whether the alive enemies are alterted upon level change
		cleanPhotonObjects();
		if(restartTime >= restartDelay){
			
			winImage.color = zero;
			winText.color  = zero;

			PhotonNetwork.LeaveRoom();
			if(level <1){
				/*Application.*/PhotonNetwork.LoadLevel(++level);
			}
			else{
				PhotonNetwork.LoadLevel(--level);
				//Debug.Log("level: "+level+"--");
			}
			Debug.Log ("Resetting colours");
		}
	}

	static void cleanPhotonObjects()
	{//http://blog.diabolicalgame.co.uk/2014/04/avoid-photon-ondestroy-warnings.html
		PhotonView[] pViews = GameObject.FindObjectsOfType<PhotonView>();

		foreach(PhotonView view in pViews)
		{
			GameObject isMineGO = view.gameObject;

			if(isMineGO != null && view.isMine && PhotonNetwork.networkingPeer.instantiatedObjects.ContainsKey(view.instantiationId))//client destroying own gameobject
			{//the second clause is due to an error occuring with photon network trying to remove the enemies (the dont have an instantiation id
				//because they arn't phonnetwork instantiated)
				PhotonNetwork.Destroy(isMineGO);
			}
			else
			{
				if(PhotonNetwork.networkingPeer.instantiatedObjects.ContainsKey(view.instantiationId))
				{//if is not this clients object remove its key- don't worry about it anymore
					PhotonNetwork.networkingPeer.instantiatedObjects.Remove(view.instantiationId);
				}
			}
		}
		PhotonNetwork.SetSendingEnabled (0, false);
		PhotonNetwork.isMessageQueueRunning = false;
	}
	public int returnPlayerTime(){
		return (int)((timerMin*60)+timerSec);
	}
}