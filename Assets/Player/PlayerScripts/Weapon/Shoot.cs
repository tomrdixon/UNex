using UnityEngine;
using System.Collections;

public class Shoot : MonoBehaviour {
	public LineRenderer shootLine;
	public bool reloading = false;

	private Animator anim;
	private Health hitGOHealth;//ammo size will never be big enough to justify full int
	private	Transform shootExit;
	private	Vector3 shootExitPos;
	private WeaponManager weapon; 
	private float fRateCool;
	private GameObject muzzleFlash;
	private GameObject[] shotgunExits;
	private bool firing = false;


	//private float fRate;

	void Start()
	{
		shotgunExits = GameObject.FindGameObjectsWithTag ("ShotgunExit");

		weapon = gameObject.GetComponent<WeaponManager> ();
		//weapon.currentWeapon.clipcount = weapon.currentWeapon.GetClipSize ();


	}

	void Update()
	{

		KeepGunCurrent ();

		if(Input.GetButton("Fire1") && (fRateCool<0) && weapon.currentWeapon.clipcount >0 && !reloading && !firing){
			StartCoroutine( Fire());}//is an ienumerator so that a delay can occur with the shotgun
//		if (weapon.currentWeapon.fireRateCoolDown >= weapon.currentWeapon.GetFireRate()) {
//			shootLine.enabled = false;}		
		if(!reloading && (Input.GetKey (KeyCode.R) || weapon.currentWeapon.clipcount <=0) && weapon.currentWeapon.clipcount < weapon.currentWeapon.GetClipSize()){
			StartCoroutine(ActivateAnim("Reloading", true, weapon.currentWeapon.GetReloadTime()));}

		if (reloading && weapon.GetWeaponChanged () == true) {
			StopCoroutine("ActivateAnim");
			reloading = false;
			if(anim)
				anim.SetBool ("Reloading", false);
			weapon.currentWeapon.clipcount = weapon.currentWeapon.GetClipSize ();
			weapon.WeaponChangeComplete();
		}


	}

	void KeepGunCurrent()
	{
		shootExitPos = transform.GetChild(0).GetChild(0).position;
		shootExitPos.y -= .2f;
		shootExitPos.x +=  1.3f * transform.rotation.x;
		shootExit = transform;

		anim = transform.parent.GetComponentInChildren<Animator>();
		//assigning anim the value of the animator available in the currentweapon. Ensuring that it is kept current
		weapon.currentWeapon.fireRateCoolDown -= Time.deltaTime;//decrementing 
		fRateCool = weapon.currentWeapon.fireRateCoolDown;//these exist purley for legibility 
		//(ensures that the ammo will change as soon as the weapon has)
		if(muzzleFlash){
			muzzleFlash.transform.parent = transform.GetChild(0);}
	}

	IEnumerator Fire()
	{


		StartCoroutine (ActivateAnim ("Shooting", false, weapon.currentWeapon.GetFireRate ()));//starting the animation

		firing = true;
		yield return new WaitForSeconds (weapon.currentWeapon.GetWaitBeforeInitFireTime());//delaying firing the weapon until specified time
		firing = false;

		weapon.currentWeapon.clipcount --;//decrementing amount of ammo gun has in clip
		weapon.currentWeapon.fireRateCoolDown = weapon.currentWeapon.GetFireRate();// reseting the fire rate cooldown

		if(weapon.currentWeapon.GetShootEffect()){//setting the effect
			//shooting the rocket further forward if the weapon is a rocket
			if(weapon.currentWeapon.GetWeaponType() == Weapon.WeaponType.Projectile){
				Instantiate (weapon.currentWeapon.GetShootEffect(), shootExitPos + (shootExit.forward*0.1f), Camera.main.transform.rotation);
				yield break;//if is the rocket launcher then the rocket will deal damage etc.
			}
			//giving the end of the gun sparks - doing both of these things regardless of whether anything is hit by the raycast
			Debug.Log("found gun end "+transform.FindChild("GunEnd"));
			muzzleFlash = (GameObject) Instantiate(weapon.currentWeapon.GetShootExitBarrel(),transform.GetChild(0).GetChild(0).position,shootExit.transform.rotation);
			muzzleFlash.transform.parent = transform.parent;
		}

		if (weapon.currentWeapon.GetWeaponType() == Weapon.WeaponType.SingleShot) {//shooting the ray

			Vector3 rayForward = new Vector3(Camera.main.transform.forward.x+.002f,Camera.main.transform.forward.y,Camera.main.transform.forward.z);
			//Vector3 rayPos = new Vector3(campos.x + (-.4f),campos.y+(2f),campos.z);
			for(int i=0;i<shotgunExits.Length;i++)
			{
				DrawRay(shotgunExits[i].transform.forward, (weapon.currentWeapon.GetDamage()/shotgunExits.Length));
			}
		}else{
			DrawRay (Camera.main.transform.forward, weapon.currentWeapon.GetDamage());
		}




	}
	void DrawRay(Vector3 rayForward, float damage)
	{
		Ray ray = new Ray(Camera.main.transform.position, rayForward);
		RaycastHit hitInfo;
		
		if (Physics.Raycast (ray, out hitInfo, 100f)) {
			//Debug.Log("have shot");
			if(weapon.currentWeapon.GetShootEffect()){
				Instantiate(weapon.currentWeapon.GetShootEffect(),hitInfo.point,Quaternion.identity);}
			GameObject gO = hitInfo.collider.gameObject;//getting the hit gameobject
			
			hitGOHealth =gO.transform.root.GetComponent<Health>();
			//Debug.Log("root is: "+gO.transform.root.name);
			if(hitGOHealth){
				if(!hitGOHealth.transform.root.GetComponent<PhotonView>())
				{	//Debug.Log("No photonview copmonent found of this game object");
					//Debug.Log("gameobject found: "+ hitGOHealth.transform.root.name);
				}
				else{

					try{
						if(PhotonNetwork.offlineMode){
							hitGOHealth.TakeDamage(gO, damage);}
						else{
							hitGOHealth.GetComponent<PhotonView>().RPC("TakeDamage",PhotonTargets.All,gO, damage);//RPC is global method, am invoking it on the photonview component
						}
					}
					catch(System.Exception ex)
					{
						Debug.Log(ex);
					}
					}//of the hit object, i.e. sending the message to every object with a photonview component
			}
			//shootLine.SetPosition(1,hitInfo.point);
		}
		Debug.DrawRay(transform.position,transform.forward,Color.blue,10000f,false);
	}

	IEnumerator ActivateAnim(string boolName, bool isReloading, float waitTime)
	{
		if(anim !=null){ //making sure that the animator is assigned
			if(isReloading){
				reloading = true;//saying that am currently reloading
			}//set the appropriate animator bool to true
			anim.SetBool (boolName, true);
			}
		yield return new WaitForSeconds (waitTime);

		if(anim)
			anim.SetBool (boolName, false);

		if(isReloading){
			reloading = false;//state that reloading has finished, if this was used for reloading, and reset ammo
			weapon.currentWeapon.clipcount = weapon.currentWeapon.GetClipSize ();
		}

	}

	public short GetCurrentAmmo()
	{
		return weapon.currentWeapon.clipcount;
	}

}
