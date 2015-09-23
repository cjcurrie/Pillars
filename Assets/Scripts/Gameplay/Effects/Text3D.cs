using UnityEngine;
using System.Collections;

public class Text3D : MonoBehaviour {

  Transform myTrans;

	void Start () {
	 myTrans = transform;
	}

  void Update()
  {
    if (!GameController.initialized)
      return;

    else
    {
      myTrans.rotation = Quaternion.LookRotation(myTrans.position - GameController.camTrans.position);
    }
  }

}
