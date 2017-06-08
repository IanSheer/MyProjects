// This script handles UI panels behavior and equip/unequip functions
// Scripted by Ivan Ovchinnikov for M.A.C.project (c), 2016.

using UnityEngine;
using System.Collections;

public enum ItemType { Head, Body }
[System.Serializable]
public class Item {
	public int Id;
	public Sprite Icon;
	public GameObject Mesh;
	public ItemType Type;
	public int Animation;
	public string SayGive;
	public string SayTake;
	public int RandomAnimation;
}

public class Toolsheet : MonoBehaviour {
	public Item[] Items;
	public static int Tool = 0;
	public static bool Hold = false;
	private Animator ItemPan;
	private Animator ActPan;
	private Animator Cub;
	private GameObject ItemInst;
	private GameObject ItemHead;
	private GameObject ItemBody;
	private Item helditm;
	private float touch;
	private bool swept;
	public Transform Itm;
	public GameObject ItemButton;
	private GameObject[] IBinst;
	public static int headEq;
	public static int bodyEq;

	void Start () {
		ItemPan = GameObject.Find ("PanelItems").GetComponent<Animator> ();
		ActPan = GameObject.Find ("PanelActivities").GetComponent<Animator> ();
		Cub = GameObject.Find ("/Cub").GetComponent<Animator> ();
		DrawItemButtons ();
	}

	// Instantiates UI buttons for items panel
	void DrawItemButtons () {
		int i = 0;
		Transform ip = GameObject.Find ("PanelItems/Viewport/Content").transform;
		IBinst = new GameObject[Items.Length];
		for (i = 0; i < Items.Length; i++) {
			Items [i].Id = i;
			IBinst [i] = Instantiate (ItemButton);
			IBinst [i].transform.SetParent (ip);
			IBinst [i].transform.localScale = Vector3.one;
			IBinst [i].transform.Find ("Icon").GetComponent<UnityEngine.UI.Image> ().sprite = Items [i].Icon;
			int ib = i;
			UnityEngine.EventSystems.EventTrigger et = IBinst [i].GetComponent<UnityEngine.EventSystems.EventTrigger> ();
			UnityEngine.EventSystems.EventTrigger.Entry entry = new UnityEngine.EventSystems.EventTrigger.Entry ();
			entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
			entry.callback.AddListener (( data) => {
				HoldItem ((int)ib);
			});
			et.triggers.Add (entry);
		}
	}

	//Pull out left or right panel
	public void DrawTools (int tid) {
		if (tid == 1 && !Interact.input) {
			if (Tool == 0 || Tool == 2) {
				if (Tool == 2)
					ActPan.SetBool ("Active", false);
				ItemPan.SetBool ("Active", true);
				Tool = 1;
				MicHandle.Hide ();
			} else { 
				ItemPan.SetBool ("Active", false);
				Tool = 0;
				MicHandle.Show ();
			}
		}
		if (tid == 2 && !Interact.input) {
			if (Tool == 0 || Tool == 1) {
				if (Tool == 1)
					ItemPan.SetBool ("Active", false);
				ActPan.SetBool ("Active", true);
				Tool = 2;
				MicHandle.Hide ();
			} else { 
				ActPan.SetBool ("Active", false);
				Tool = 0;
				MicHandle.Show ();
			}
		}
	}

	//Hide both panels
	public void ClearTools () {
		if (Tool > 0) {
			ActPan.SetBool ("Active", false);
			ItemPan.SetBool ("Active", false);
			Tool = 0;
		}
		MicHandle.Show ();
	}

	//Instantiates item (item id) to hold
	void HoldItem (int iid) {
		if (IBinst [iid].GetComponent<UnityEngine.UI.Button> ().interactable) {
			ItemPan.SetBool ("Active", false);
			Tool = 0;
			TimeMan.AdjustColor (Items [iid].Mesh);
			ItemInst = Instantiate (Items [iid].Mesh);
			ItemInst.transform.SetParent (Itm);
			ItemInst.transform.localPosition = Vector3.zero;
			ItemInst.transform.localScale = new Vector3 (4, 4, 4);
			ItemInst.transform.localRotation = Quaternion.identity;
			helditm = Items [iid];
			Hold = true;
		}
		return;
	}

	//Instantiates item(it) to the cub's body.
	void Equip (Item it) {
		if ((it.Type == ItemType.Body && ItemBody) || (it.Type == ItemType.Head && ItemHead) || TimeMan.CurrentDT == "Night") return;
		IBinst [it.Id].GetComponent<UnityEngine.UI.Button> ().interactable = false;
		if (it.Type == ItemType.Body) {
			if (ItemBody) Destroy (ItemBody); 
			ItemBody = Instantiate (it.Mesh);
			ItemBody.transform.SetParent (GameObject.Find ("/Cub/Armature/Pelvis/Spine1/Spine2/Shoulder.R/UpprArm.R/ForeArm.R/Hand.R/Hold").transform);
			ItemBody.transform.localPosition = Vector3.zero;
			ItemBody.transform.localScale = new Vector3 (4, 4, 4);
			ItemBody.transform.localRotation = Quaternion.identity;
			Toolsheet.bodyEq = it.Id + 1;
			TimeMan.AdjustColor (ItemBody);
		}
		if (it.Type == ItemType.Head) {
			if (ItemHead) Destroy (ItemHead);
			ItemHead = Instantiate (it.Mesh);
			ItemHead.transform.SetParent (GameObject.Find ("/Cub/Armature/Pelvis/Spine1/Spine2/Neck/Head/Jaw/Month").transform);
			ItemHead.transform.localPosition = Vector3.zero;
			ItemHead.transform.localScale = new Vector3 (4, 4, 4);
			ItemHead.transform.localRotation = Quaternion.identity;
			Toolsheet.headEq = it.Id + 1;
			TimeMan.AdjustColor (ItemHead);
		}
	}

	void Update () {
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit;

		// Equip item when released 
		if (!Input.GetButton ("Fire1") && Physics.Raycast (ray, out hit) &&
		    (hit.collider.name == "Spine1" || hit.collider.name == "Head") && helditm != null) {
			Equip (helditm);
			Cub.SetInteger ("Grab", 0);
			if (helditm.RandomAnimation != 0) {
				Interact.RandomAct = helditm.RandomAnimation;
				Cub.SetInteger ("Act", helditm.RandomAnimation);
			}
			if (helditm.SayGive != "" && TimeMan.CurrentDT != "Night") {
				Messages.DrawMessage (helditm.SayGive);
			}
		}

		// Take item away
		if (Input.GetButton ("Fire1") && Physics.Raycast (ray, out hit) && !Hold && !Interact.input) { 
			if (hit.collider.name == "Spine1" && Toolsheet.bodyEq > 0) { 
				IBinst [Toolsheet.bodyEq - 1].GetComponent<UnityEngine.UI.Button> ().interactable = true;
				HoldItem (Toolsheet.bodyEq - 1);
				Destroy (ItemBody);
				Toolsheet.bodyEq = 0;
			}
			if (hit.collider.name == "Head" && Toolsheet.headEq > 0) { 
				IBinst [Toolsheet.headEq - 1].GetComponent<UnityEngine.UI.Button> ().interactable = true;
				HoldItem (Toolsheet.headEq - 1);
				Destroy (ItemHead);
				Toolsheet.headEq = 0;
			}
			if (helditm != null && helditm.SayTake != "" && TimeMan.CurrentDT != "Night") {
				Cub.SetInteger ("Act", 6);
				Messages.DrawMessage (helditm.SayTake);
			}
			Interact.RandomAct = 5;
		}

		//Equip items if Eq greater than 0
		if (!ItemHead && headEq > 0) Equip(Items[Toolsheet.headEq - 1]);
		if (!ItemBody && bodyEq > 0) Equip(Items[Toolsheet.bodyEq - 1]);

		// Destroy item when released outside of triggers
		if (!Input.GetButton ("Fire1") && ItemInst) {
			Destroy (ItemInst);
			helditm = null;
			Hold = false;
			Itm.localPosition = new Vector3(0,0,5);
		}

		// Swipe control section
		if (Input.GetButtonUp ("Fire1") && touch != 0) {
			touch = 0;
			swept = false;
		}
		if (Input.GetButton ("Fire1") && !Hold && !swept) {
			if (touch == 0) {
				touch = Input.mousePosition.x;
			} else if (Input.mousePosition.x > (touch + 300)) {
				DrawTools (2);
				swept = true;
			} else if (Input.mousePosition.x < (touch - 300)) {
				DrawTools (1);
				swept = true;
			}
		}

		// Move item closer when over the trigger
		float x = (Input.mousePosition.x - Screen.width / 2)/100;
		float y = (Input.mousePosition.y - Screen.height / 2)/100;
		float z = Itm.localPosition.z;
		if (Input.GetButton ("Fire1") && Hold) {
			if (Physics.Raycast (ray, out hit) && helditm != null) {
				if (hit.collider.name == "Spine1" && helditm.Type == ItemType.Body && !ItemBody && TimeMan.CurrentDT != "Night") {
					z = Mathf.Lerp (z, 9F, 0.5F);
					Cub.SetInteger ("Grab", 1);
				} else if (hit.collider.name == "Head" && helditm.Type == ItemType.Head && !ItemHead && TimeMan.CurrentDT != "Night") {
					z = Mathf.Lerp (z, 10F, 0.5F);
					Cub.SetInteger ("Grab", 2);
				} else if (Itm.localPosition.z > 5.1F) {
					z = Mathf.Lerp (z, 5F, 0.5F);
					Cub.SetInteger ("Grab", 0);
				}
			}
		}
		Itm.localPosition = new Vector3 (x, y, z);
	}
}
