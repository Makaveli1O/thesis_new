using UnityEngine;

/// <summary>
/// Class handling aoe spell
/// </summary>
public class AoeSpell : MonoBehaviour
{
    [SerializeField] private GameObject vfxPrefab;
    [SerializeField] private LineRenderer lr;
    private Vfx vfxScript;
    private float radius;
    private float offset = (Mathf.Sqrt(2)/2);
    private void Awake() {
        vfxScript = vfxPrefab.GetComponent<Vfx>();
    }

    /// <summary>
    /// Performs this aoe attack.
    /// </summary>
    /// <param name="time">Duration of the spell(distance travelled)</param>
    public void Perform(float time, int damage){
        
        Vector3[] directions = new [] {Vector3.left, (Vector3.left + Vector3.up) * offset, Vector3.up, (Vector3.up + Vector3.right) * offset , Vector3.right, (Vector3.right + Vector3.down) * offset , Vector3.down, (Vector3.down + Vector3.left) * offset};
        //spawn tornado in 8 directions
        foreach (Vector3 direction in directions)
        {
            GameObject vfxObj = Instantiate(vfxPrefab, this.transform.position + direction, Quaternion.identity);
            vfxObj.transform.parent = this.transform;
            Vfx tornado = vfxObj.GetComponent<Vfx>();
            tornado.SetVFXType(Vfx.ElementType.Wind);
            tornado.SetDamageAmount(damage);
            tornado.Move(direction, time);   //move tornado
        }
    }
    /*
    R A D I U S  C I R C L E
    */
    /// <summary>
    /// Shows radius around player for spell's reach
    /// </summary>
    public void ShowGuidelines(){
        lr.enabled = true;
        float normalize = 2f;
        lr.positionCount = 16;
        //left
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, Vector3.left / normalize);
        //top-left
        lr.SetPosition(2, Vector3.zero);
        lr.SetPosition(3, ((Vector3.left + Vector3.up) * offset) / normalize);
        //top
        lr.SetPosition(4, Vector3.zero);
        lr.SetPosition(5, Vector3.up / normalize);
        //top-right
        lr.SetPosition(6, Vector3.zero);
        lr.SetPosition(7, ((Vector3.up + Vector3.right) * offset) / normalize);
        //right
        lr.SetPosition(8, Vector3.zero);
        lr.SetPosition(9, Vector3.right / normalize);
        //bot-right
        lr.SetPosition(10, Vector3.zero);
        lr.SetPosition(11, ((Vector3.down + Vector3.right) * offset) / normalize);
        //bot
        lr.SetPosition(12, Vector3.zero);
        lr.SetPosition(13, Vector3.down / normalize);
        //bot-left
        lr.SetPosition(14, Vector3.zero);
        lr.SetPosition(15, ((Vector3.down + Vector3.left) * offset) / normalize);

        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        GameAssets.Instance.cursorHandler.SetCursorByType(CursorType.Apply);
    }

    public void HideGuidelines(){
        lr.enabled = false;
        GameAssets.Instance.cursorHandler.SetCursorByType(CursorType.Basic); 
    }
}
