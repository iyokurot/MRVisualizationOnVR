using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayPointer : MonoBehaviour {
    [SerializeField]
    Transform rightHandAnchor;

    [SerializeField]
    private Transform leftHandAnchor;

    [SerializeField]
    private Transform centerEyeAnchor;

    [SerializeField]
    private float maxDistance = 100.0f;

    [SerializeField]
    private LineRenderer laserPointerRenderer;
    // Start is called before the first frame update
    void Start () {

    }

    // Update is called once per frame
    void Update () {
        var pointer = Pointer;
        if (pointer == null || laserPointerRenderer == null) {
            return;
        }
        Ray pointerRay = new Ray (pointer.position, pointer.forward);
        //Layの起点
        laserPointerRenderer.SetPosition (0, pointerRay.origin);
        RaycastHit hitInfo;
        if (Physics.Raycast (pointerRay, out hitInfo, maxDistance)) {
            //RayがHit
            laserPointerRenderer.SetPosition (1, hitInfo.point);
        } else {
            laserPointerRenderer.SetPosition (1, pointerRay.origin + pointerRay.direction * maxDistance);
        }
    }
    private Transform Pointer {
        get {
            var controller = OVRInput.GetActiveController ();
            Debug.Log (controller);
            if (controller == OVRInput.Controller.RTouch) {
                return rightHandAnchor;
            } else if (controller == OVRInput.Controller.LTouch) {
                return leftHandAnchor;
            } else if (controller == OVRInput.Controller.Touch) {
                return rightHandAnchor;
            }
            // どちらも取れなければ目の間からビームが出る
            return centerEyeAnchor;
        }
    }
}