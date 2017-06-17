// Render 360 degrees from corresponding game object transform to 6 sides PNGs.
// To manually start and stop rendering, call Render360Camera.StartRender()
// and Render360Camera.StopRender respectively.
// Custom camera prefab can be attached (if you need to render with image effects),
// Otherwise new game object with the camera component will be created.
// Scripted by Ivan Ovchinnikov, 2017

using UnityEngine;
using System.Collections;
using System;
using System.IO;

public class RenderBytes {
	public static void Render(byte[] rend, string name) {
		string path = Application.persistentDataPath + "/Render/";
		try {
			if (!System.IO.Directory.Exists(path))
				System.IO.Directory.CreateDirectory(path);
			System.IO.File.WriteAllBytes(System.IO.Path.Combine(path,name + ".png"), rend);
			Debug.Log("Frame " + name + " rendering was OK.");
		} catch(Exception e) {
			Debug.LogWarning("RenderBytes.Render(): Failed to render " + path+name+".png" + " (Reason: " + e.ToString() + ")");
		}
	}
}

[AddComponentMenu("Rendering/360Camera")]
public class Render360Camera : MonoBehaviour {

	public int renderSize = 512;
	public int frames;
	public GameObject cameraPrefab;
	private TextureFormat format = TextureFormat.RGB24;
	public bool startRender = true;
	private Camera cam;
	private RenderTexture rt;
	private GameObject rc;
	private Vector3[] sides = new Vector3[6];
	private string[] pointers = { "f", "r", "b", "l", "u", "d" };
	private int fcounter = 0;

	void Start() {
		if (frames == 0)
			Debug.Log ("Invalid number of frames");
		if (renderSize < 1)
			Debug.Log ("Render size must be greater than 0!");
		sides [0] = new Vector3 (0, 0, 0);
		sides [1] = new Vector3 (0, 90, 0);
		sides [2] = new Vector3 (0, 180, 0);
		sides [3] = new Vector3 (0, -90, 0);
		sides [4] = new Vector3 (-90, 0, 0);
		sides [5] = new Vector3 (90, 0, 0);
		if (cameraPrefab && cameraPrefab.GetComponent<Camera> ())
			rc = Instantiate (cameraPrefab);
		else
			rc = new GameObject ("RenderCamera", typeof(Camera));		
		rc.hideFlags = HideFlags.HideAndDontSave;
		cam = rc.GetComponent<Camera> ();
		cam.backgroundColor = Color.black;
		cam.clearFlags = CameraClearFlags.Skybox;
		cam.fieldOfView = 90;
		cam.aspect = 1.0f;
		cam.enabled = false;
		rc.transform.position = transform.position; //Camera.main.gameObject.transform.position;
		rc.transform.rotation = transform.rotation; //Camera.main.gameObject.transform.rotation;
	}

	public static void StartRender() {
		Render360Camera rendcam = (Render360Camera) FindObjectOfType(typeof(Render360Camera));
		if (!rendcam.startRender)
			rendcam.startRender = true;
	}

	public static void StopRender() {
		Render360Camera rendcam = (Render360Camera) FindObjectOfType(typeof(Render360Camera));
		if (rendcam.startRender)
			rendcam.startRender = false;
	}

	void LateUpdate () {
		if (frames == 0 || renderSize < 1)
			return;
		if (startRender) {
			if (fcounter < frames) {
				string fileName = fcounter.ToString ();
				if (fcounter < 1000)
					fileName = 0 + fileName;
				if (fcounter < 100)
					fileName = 0 + fileName;
				if (fcounter < 10)
					fileName = 0 + fileName;
				rc.transform.position = transform.position; //Camera.main.gameObject.transform.position;
				int i = 0;
				for (i = 0; i < sides.Length; i++)
					RenderSide (transform.eulerAngles + sides [i], pointers [i], fileName);
				fcounter++;
			} else {
				if (rc) {
					cam = null;
					DestroyImmediate (rc);
					string path = Application.persistentDataPath + "/Render/";
					Debug.Log ("Rendering has been finished.\nAll the frames were saved to " + path);
					if (Application.platform == RuntimePlatform.WindowsEditor) {
						path = path.Replace ("/", "\\");
						System.Diagnostics.Process.Start ("explorer.exe", "/open, " + path);
					}
					if (Application.platform == RuntimePlatform.OSXEditor)
						System.Diagnostics.Process.Start ("open", "\"" + path + "\"");
				}
			}
		}
	}

	void RenderSide (Vector3 dir, string point, string count) {
		rc.transform.eulerAngles = dir;
		rt = new RenderTexture (renderSize, renderSize, 24);
		rt.hideFlags = HideFlags.HideAndDontSave;
		cam.targetTexture = rt;
		Texture2D frame = new Texture2D (rt.width, rt.height, format, false);
		cam.Render ();
		RenderTexture.active = rt;
		frame.ReadPixels (new Rect (0, 0, frame.width, frame.height), 0, 0);
		RenderTexture.active = null;
		cam.targetTexture = null;
		DestroyImmediate (rt);
		byte[] bytes = frame.EncodeToPNG ();
		count = point + "_" + count;
		RenderBytes.Render (bytes, count);
	}
}
