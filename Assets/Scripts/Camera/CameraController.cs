using UnityEngine;
using System.Collections;
//TODO comment this 
public class CameraController : MonoBehaviour
{
    public GameObject player;
    public GameObject mapObj;
    private Camera mainCamera;
    private Map map;
    
    private float maxDistance = 10; // maximum permitted distance of camera from player X,Y Axis
    private float maxCameraHeight = 2.0f; //maximum camera distance
    private float minCameraHeight = 1.0f; //minimum camera distance

    private float cameraSpeed = 20f;

    private float moveX;
    private float moveY;

    void Start(){
        mainCamera = GetComponent<Camera>();
        mainCamera.orthographicSize = 10.0f;
        map = mapObj.GetComponent<Map>();
        StartCoroutine(WaitForGenerationToFinish());
    }

    /// <summary>
    /// Move camera to player's position instantly ater generation is done
    /// </summary>
    /// <returns></returns>
    private IEnumerator WaitForGenerationToFinish(){
        yield return new WaitUntil(()=>map.generationComplete);
        this.transform.position = new Vector3(player.transform.position.x,player.transform.position.y, -2f);
    }

    void Update(){
        moveX = (player.transform.position.x - this.transform.position.x) / maxDistance;
        moveY = (player.transform.position.y - this.transform.position.y) / maxDistance;
        //ScrollCameraHandler();

        this.transform.position += new Vector3((moveX * cameraSpeed * Time.deltaTime) , (moveY * cameraSpeed * Time.deltaTime) ,0);
    }

    /*
        Scrolling camera handler. Defines height of the main camera,
        scaling the orthographicSize of main camera.
    */
    void ScrollCameraHandler(){
        //Input.mouseScrollDelta returns -1 for scroll down, and 1 for scroll up
        Vector2 scrolledUp = new Vector2(0.0f, 1.0f);
        Vector2 scrolledDown = new Vector2(0.0f, -1.0f);

        if(Vector2.Distance(Input.mouseScrollDelta, scrolledUp) == 0 && mainCamera.orthographicSize <= maxCameraHeight){
            mainCamera.orthographicSize += 0.2f;
        }
        if(Vector2.Distance(Input.mouseScrollDelta, scrolledDown) == 0 && mainCamera.orthographicSize > minCameraHeight){
            mainCamera.orthographicSize -= 0.2f;
        }
    }
}
