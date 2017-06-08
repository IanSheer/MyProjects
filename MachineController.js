// Machine controller script/
//(c) Ivan Ovchinnicov, 2017./

#pragma strict
var rolgang : Transform[];
var cutterDisc : Transform;
var grip : Transform;
var cutter : Transform;
var ruler : Transform;
var wheel : Transform;
var lights : GameObject;
@Range(0,1000)
var cutterSpeed : float;
var detail : Transform;
var detailCut : GameObject;
var sparks : GameObject;
var sfx : AudioClip[];
var sources : AudioSource[];
var power : boolean;
var paused : boolean;
var playStages : boolean;
@TextArea
var messages : String[];
var phase : int = 0;
private var cut : boolean;
static var controls : boolean;
private var wheelctrl : boolean = false;
private var hinted : boolean = false;

function DrawPhaseDesc (p : int) {
	Gui.message = messages[p-1];
}

function SetTrigger(name : String, state : boolean) {
	if (name == "ruler" && phase == 1) phase = 2;
	if (name == "grip" && phase == 2) phase = 3;
	if (name == "cutter" && phase == 3) phase = 4;
	if (playStages) { paused = true; DrawPhaseDesc (phase); }
}

function SetRuler(val : float) {
	if ((val < 0 && ruler.localPosition.x > -2.25) || (val > 0 && ruler.localPosition.x < -1.5)) {
		if (ruler.eulerAngles.x <= 280) {
			wheel.Rotate(Vector3.up * val * 10);
			ruler.Translate(Vector3.right * val / 100);
		}
	}
}

function PlayMotorFx() {
	sources[0].clip = sfx[1];
	sources[0].loop = true;
	sources[0].Play();
	paused = false;
}

function StopMotorFx() {
	sources[0].Stop();
	sources[0].clip = null;
}

function PowerUp() {
	power = !power;
	if (power) {
		CancelInvoke("StopMotorFx");
		sources[0].clip = sfx[0];
		lights.SetActive(true);
		paused = true;
		Invoke("PlayMotorFx", sfx[0].length);
	} else {
		CancelInvoke("PlayMotorFx");
		sources[0].clip = sfx[3];
		sources[0].loop = false;
		sources[1].Stop();
		sources[1].clip = null;
		sources[2].Stop();
		sources[2].clip = null;
		lights.SetActive(false);
		Invoke("StopMotorFx", sfx[3].length);
	}
	sources[0].Play();
}

function CheckButtons() {
	if (!CamController.zooming) {
		var ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		var hit : RaycastHit;
		if (Input.GetKeyDown (KeyCode.Mouse0) && Physics.Raycast (ray, hit)) {
			if (hit.collider.name == "Power") { gameObject.Find("/GUI").GetComponent.<Gui>().PowerUp();
			controls = true; }
			if (hit.collider.name == "Wheel") {
				wheelctrl = true;
				controls = true; 
			}
		}
		if (Physics.Raycast (ray, hit)) { 
			if (hit.collider.name == "Power" && !Gui.hint) {
				gameObject.Find("/GUI").GetComponent.<Gui>().DrawHint(2);
				hinted = true; }
			else if (hit.collider.name == "Wheel" && !Gui.hint) {
				gameObject.Find("/GUI").GetComponent.<Gui>().DrawHint(6);
				hinted = true; }
			else if (hit.collider.name == "Main" && hinted && Gui.hint) {
				gameObject.Find("/GUI").GetComponent.<Gui>().ClearHint();
				hinted = false; }
		}
		if (wheelctrl) SetRuler(Input.GetAxis("Mouse Y")); 
		if (Input.GetKeyUp (KeyCode.Mouse0) && controls) { controls = false; wheelctrl = false; }
	}
}

function CutAt(obj : Transform, size : float) {
	var prevsize = obj.localScale.x;
	obj.localScale.x = size * 10;
	var dc = Instantiate(detailCut);
	dc.transform.position = Vector3 (0, obj.position.y, obj.position.z);
	dc.transform.localScale = Vector3 ((prevsize - size * 10) * 10, 100, 100);
	dc.transform.rotation = Quaternion.identity;
}

function Update () {
	CheckButtons();
	if (power) {
		if (phase == 0) phase = 1;
		if (phase == 1 && !paused) {
			for (var r in rolgang) r.Rotate(Vector3.up * Time.deltaTime * 100);
			if (ruler.eulerAngles.x < 330) ruler.Rotate(Vector3.right * Time.deltaTime * 50);
			if (detail.position.x < 0.45) detail.gameObject.GetComponent.<Rigidbody>().isKinematic = false;
			else detail.Translate(-Vector3.right * Time.deltaTime / 4);
			cut = false;
		} else if (phase == 2) {
			if (grip.position.y > 2.25 && !paused) grip.Translate(-Vector3.forward * Time.deltaTime / 4);
			cutterSpeed = Mathf.Lerp(cutterSpeed, 1000, 0.2f);
			if (!sources[1].clip) {
				sources[1].clip = sfx[1];
				sources[1].loop = true;
				sources[1].Play();
			}
		} else if (phase == 3) {
			if (!paused) {
				if (ruler.eulerAngles.x > 280) ruler.Rotate(-Vector3.right * Time.deltaTime * 50);
				cutter.Rotate(Vector3.right * Time.deltaTime);
				if (cutter.eulerAngles.x < 292 && !cut) { CutAt(detail, detail.position.x); cut = true; }
			}
			if (cutter.eulerAngles.x < 298 && cutter.eulerAngles.x > 292 && !paused) { 
				sparks.SetActive(true);
				if (!sources[2].clip) {
					sources[2].clip = sfx[2];
					sources[2].loop = true;
					sources[2].Play();
				}
			} else { 
				sparks.SetActive(false);
				if (sources[2].clip != null) {
					sources[2].Stop();
					sources[2].clip = null;
				}
			}
		} else if (phase == 4 && !paused) {
			if (cutter.eulerAngles.x < 305) { cutter.Rotate(-Vector3.right * Time.deltaTime * 10); } 
			else { phase = 5; if (playStages) { paused = true; DrawPhaseDesc (phase); } }
		} else {
			if (!paused) {
				cutterSpeed = Mathf.Lerp(cutterSpeed, 0, 0.1f);
				if (grip.position.y < 2.5) { grip.Translate(Vector3.forward * Time.deltaTime / 4); } 
				else { phase = 1; if (playStages) { paused = true; DrawPhaseDesc (phase); } }
			}
			if (sources[1].clip != null) {
				sources[1].Stop();
				sources[1].clip = null;
			}
		}
		cutterDisc.Rotate(Vector3.right * Time.deltaTime * cutterSpeed);
	} else {
		if (sparks && sparks.activeInHierarchy == true) sparks.SetActive(false);
	}
}