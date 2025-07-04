using UnityEngine;
using Unity.Mathematics;
using System.Collections;


/// <summary>
/// Class that handles interaction between player and generated world.
/// Checks player's position every frame, and determines whenever player
/// can move to certain tile or not.
/// </summary>
public class MapController : MonoBehaviour
{
    BoxCollider2D col;
    Vector3 lastPos;
    Vector3 playerPos;
    Map mapObj;
    private GameObject playerObj = null;
    private GameHandler gameHandler = null;
    private PlayerController playerController;
    private bool generationDone = false;
    private Vector3 lastMarkedPos; //used when detecting tile edge collision

    //reworked instead start, and awake
    public void InitMapController()
    {
        if (playerObj == null)          //get player obj
            playerObj = GameObject.Find("Player");

        //beggining position ( spawn )
        try{
            playerController = playerObj.GetComponent<PlayerController>();
            SavePosition playerSave = gameHandler.Load<SavePosition>(ObjType.Player);
            playerPos = playerSave.pos;
            playerObj.transform.position = playerPos;
            playerController.LoadedHealth(playerSave.healthAmount);
            playerController.LoadedShieldHealth(playerSave.shieldAmount);
        }catch{
            //first encounter
            TDTile spawnableTile = mapObj.GetSpawableTile(new int2(32,32), new int2(96, 96));
            //Debug.Log("Spawning on: " + spawnableTile.pos);
            playerController.transform.position = new Vector3(spawnableTile.pos.x, spawnableTile.pos.y);
        }

        BoxCollider2D col = mapObj.GetComponent<BoxCollider2D>();
    }

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        mapObj = GetComponent<Map>();   //get reference to map
        gameHandler = GetComponent<GameHandler>();
        StartCoroutine(WaitForGenerationToFinish());
    }

    void Update()
    {
        if (generationDone)
        {
            int xPos = (int)playerObj.transform.position.x;
            int yPos = (int)playerObj.transform.position.y;
            int zPos = (int)playerObj.transform.position.z;

            int2 chunkKey = mapObj.TileChunkPos(new int2(xPos, yPos));
            int2 relativePos = mapObj.TileRelativePos(new int2(xPos, yPos));

            TDTile tile = mapObj.GetTile(relativePos, chunkKey);
            
            if (tile.IsWalkable) {
                playerPos = new Vector3(playerObj.transform.position.x,playerObj.transform.position.y,0);
            //non-walkable tiles
            }else{
                if(tile.partial){
                    TileEdgesCollision(tile,playerPos);
                }else{  //full cliffs unable to walk every where
                    playerObj.transform.position = playerPos; // stop
                }
            }
        }

        //different position found
        //usage of exception because playerObj is not assigned when loading scene
        try
        {
            if (!lastMarkedPos.Equals(playerObj.transform.position))
            {
                lastMarkedPos = playerObj.transform.position;
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Coroutine to wait until map is properly generated (or regenerated)
    /// </summary>
    /// <returns></returns>
    private IEnumerator WaitForGenerationToFinish(){
        yield return new WaitUntil(()=>mapObj.generationComplete);
        InitMapController();
        generationDone = true;
        gameHandler.PlayMainTheme();
    }



    void OnApplicationQuit() {
        if (!playerController.IsDead)
        {
            SavePosition savePlayer = new SavePosition(playerPos, playerController.healthSystem.GetHealth(), playerController.shield.healthSystem.GetHealth());
            gameHandler.Save<SavePosition>(savePlayer, ObjType.Player,playerPos);     
        }
    }

    /// <summary>
    /// Manually saves player's state
    /// </summary>
    public void ManualSave(){
        SavePosition savePlayer = new SavePosition(playerPos, playerController.healthSystem.GetHealth(), playerController.shield.healthSystem.GetHealth());
        gameHandler.Save<SavePosition>(savePlayer, ObjType.Player,playerPos);
        return;
    }


    /// <summary>
    /// handles collision between player and problematic tiles
    /// </summary>
    /// <param name="tile">Processing tile</param>
    /// <param name="playerPos">position of the player</param>
    private void TileEdgesCollision(TDTile tile, Vector3 playerPos){
        float threshold = 0.2f;
        float offset = 0.5f;
        EdgeType type = (tile.hillEdge != EdgeType.none) ? tile.hillEdge : tile.edgeType;
        //right Tile
        if (type == EdgeType.right){
            RightEdgeCollision(tile, type, offset, playerController.moveDir, playerPos);
        }else if (type == EdgeType.left){
            LeftEdgeCollision(tile, offset, playerController.moveDir, playerPos);
        }else if (type == EdgeType.top){
            TopEdgeCollision(tile, type, offset, playerController.moveDir, playerPos);
        }else if (type == EdgeType.bot || type == EdgeType.cliffEndBot){
            BotEdgeCollision(tile, offset, playerController.moveDir, playerPos);
        }else if (type == EdgeType.topRight){
            bool overDiagonal = isInside(new int2(tile.pos.x+1, tile.pos.y),new int2(tile.pos.x+1, tile.pos.y+1), new int2(tile.pos.x, tile.pos.y + 1),playerObj.transform.position);
            bool belowDiagonal = isInside(tile.pos, new int2(tile.pos.x+1, tile.pos.y), new int2(tile.pos.x, tile.pos.y+1),playerObj.transform.position);
            bool outsideCond = playerPos.y > tile.pos.y+1 || playerPos.x > tile.pos.x+1;
            //move up, right or upright
            bool moveCond = playerController.moveDir.y > 0 || playerController.moveDir.x > 0;
            CornerEdgesCollision(playerPos, belowDiagonal, outsideCond, moveCond, overDiagonal);
        }else if (type == EdgeType.topLeft){
            bool overDiagonal = isInside(tile.pos,new int2(tile.pos.x, tile.pos.y + 1), new int2(tile.pos.x + 1, tile.pos.y + 1),playerObj.transform.position);
            bool belowDiagonal = isInside(tile.pos, new int2(tile.pos.x+1, tile.pos.y), new int2(tile.pos.x+1, tile.pos.y+1),playerObj.transform.position);
            bool outsideCond = playerPos.y > tile.pos.y+1 || playerPos.x < tile.pos.x;
            //move up, left or upleft
            bool moveCond = playerController.moveDir.y > 0 || playerController.moveDir.x < 0;
            CornerEdgesCollision(playerPos, belowDiagonal, outsideCond, moveCond, overDiagonal);
        }else if (type == EdgeType.cliffEndRight){
            bool overDiagonal = isInside(tile.pos, new int2(tile.pos.x, tile.pos.y + 1), new int2(tile.pos.x+1, tile.pos.y+1),playerObj.transform.position);
            bool outsideCond = playerPos.y < tile.pos.y || playerPos.x > tile.pos.x +1;
            //move down, right or downright
            bool moveCond = playerController.moveDir.y < 0 || playerController.moveDir.x > 0;
            CornerEdgesCollision(playerPos, overDiagonal, outsideCond, moveCond);
        }else if (type == EdgeType.cliffEndLeft){
            bool overDiagonal = isInside(new int2(tile.pos.x, tile.pos.y + 1), new int2(tile.pos.x + 1, tile.pos.y + 1), new int2(tile.pos.x+1, tile.pos.y),playerObj.transform.position);
            bool outsideCond = playerPos.y < tile.pos.y || playerPos.x < tile.pos.x;
            //move down, left or downleft
            bool moveCond = playerController.moveDir.y < 0 || playerController.moveDir.x < 0;
            CornerEdgesCollision(playerPos, overDiagonal, outsideCond, moveCond);
        }else if (type == EdgeType.botRight){
            bool belowDiagonal = isInside(tile.pos, new int2(tile.pos.x+1, tile.pos.y), new int2(tile.pos.x+1, tile.pos.y+1),playerObj.transform.position);
            if (!belowDiagonal)
            {
                playerObj.transform.position = lastPos;
            }else{
                this.lastPos = playerObj.transform.position; //mark last position before offset
            }
        }else if (type == EdgeType.botLeft){
            bool belowDiagonal = isInside(tile.pos, new int2(tile.pos.x, tile.pos.y + 1), new int2(tile.pos.x+1, tile.pos.y),playerObj.transform.position);
            if (!belowDiagonal)
            {
                playerObj.transform.position = lastPos;
            }else{
                this.lastPos = playerObj.transform.position; //mark last position before offset
            }
        }else if(type == EdgeType.cliffRight || type == EdgeType.cliffLeft){
            lastPos = playerPos;
            playerObj.transform.position = playerPos;
        }
    }

    /// <summary>
    /// Handling movement for the right edge tile. (small portion of that is unwalkable)
    /// </summary>
    /// <param name="tile">Processed tle</param>
    /// <param name="offset">Offset of walking tile</param>
    /// <param name="dir">Direction of movement</param>
    /// <param name="playerPos">Position of the player</param>
    void RightEdgeCollision(TDTile tile, EdgeType type, float offset, Vector3 dir, Vector3 playerPos){

        if (dir.x > 0)  //moving right
        {
            if (playerObj.transform.position.x < tile.pos.x + offset) //within offset
            {
                this.lastPos = playerObj.transform.position; //mark last position before offset
            }else{  //stop
                if(dir.y != 0f){  //vertical movement
                    playerController.moveDir = Vector3.zero;
                }else{
                    //playerObj.transform.position = lastPos; // stop
                    if (Vector3.Distance(playerObj.transform.position, lastPos) <= 0.3f) playerObj.transform.position = lastPos; // stop 
                    else playerObj.transform.position = lastMarkedPos;
                }
            }
        }else if(dir.x <0){  //moving left
            if (playerPos.x > tile.pos.x +1)   //coming from the right side ( instant stop)
            {
                //calculate frictionvector
                playerController.moveDir = new Vector3(0,playerController.moveDir.y / 2f,0);
            }
        }
    }

    /// <summary>
    /// Handling movement for the left edge tile. (small portion of that is unwalkable)
    /// </summary>
    /// <param name="tile">Processed tle</param>
    /// <param name="offset">Offset of walking tile</param>
    /// <param name="dir">Direction of movement</param>
    /// <param name="playerPos">Position of the player</param>
    void LeftEdgeCollision(TDTile tile, float offset, Vector3 dir, Vector3 playerPos){
        if (dir.x > 0)  //moving right
        {
            if (playerPos.x <= tile.pos.x)   //coming from the right side ( instant stop)
            {
                //calculate frictionvector
                this.lastPos = playerObj.transform.position; //mark last position before offset
                playerController.moveDir = new Vector3(0,playerController.moveDir.y / 2f,0);
            }
        }else{  //moving left
            if (playerObj.transform.position.x > tile.pos.x + offset) //within offset
            {
                this.lastPos = playerObj.transform.position; //mark last position before offset
            }else{  //stop
                //playerObj.transform.position = lastPos; // stop
                //playerController.moveDir = Vector3.zero;
                if (Vector3.Distance(playerObj.transform.position, lastPos) <= 0.3f) playerObj.transform.position = lastPos; // stop 
                else playerObj.transform.position = lastMarkedPos;   
            }
        }
    }
    /// <summary>
    /// Handling movement for the left edge tile. (small portion of that is unwalkable)
    /// </summary>
    /// <param name="tile">Processed tle</param>
    /// <param name="offset">Offset of walking tile</param>
    /// <param name="dir">Direction of movement</param>
    /// <param name="playerPos">Position of the player</param>
    void TopEdgeCollision(TDTile tile, EdgeType type, float offset, Vector3 dir, Vector3 playerPos){
        if (dir.y > 0)  //moving up
        {
            if (playerObj.transform.position.y < tile.pos.y + offset) //within offset
            {
                this.lastPos = playerObj.transform.position; //mark last position before offset
            }else{  //stop
                //playerObj.transform.position = lastPos; // stop
                //playerController.moveDir = Vector3.zero; 
                if (Vector3.Distance(playerObj.transform.position, lastPos) <= 0.3f) playerObj.transform.position = lastPos; // stop 
                else playerObj.transform.position = lastMarkedPos;   
            }
        }else{  //moving down
            if (playerPos.y > tile.pos.y +1)   //coming from the top side
            {
                //calculate frictionvector
                playerController.moveDir = new Vector3(playerController.moveDir.x / 2f, 0,0);
            }
        }
    }

    /// <summary>
    /// Handles walkability on tiles that are consideret to be bottom of the cliff edge. 
    /// </summary>
    /// <param name="tile">Processed tile</param>
    /// <param name="offset">Offset within tile.</param>
    /// <param name="dir">Direction player is heading</param>
    /// <param name="playerPos">Position of the player.</param>
    void BotEdgeCollision(TDTile tile, float offset, Vector3 dir, Vector3 playerPos){
        if (dir.y > 0)  //moving up
        {
            if (playerPos.y < tile.pos.y)   //coming from the bottom side
            {
                //calculate frictionvector
                playerController.moveDir = new Vector3(playerController.moveDir.x / 2f, 0,0);
            }
        }else{  //moving down
            if (playerObj.transform.position.y > tile.pos.y + offset) //within offset
            {
                this.lastPos = playerObj.transform.position; //mark last position before offset
            }else{  //stop
                if (playerObj.transform.position.x == tile.pos.x || playerObj.transform.position.x == tile.pos.x + 0.99f)
                {
                    this.lastPos = playerObj.transform.position; //mark last position before offset
                }
                if (Vector3.Distance(playerObj.transform.position, lastPos) <= 0.3f) playerObj.transform.position = lastPos; // stop 
                else playerObj.transform.position = lastMarkedPos;
            }
        }
    }

    /// <summary>
    /// Handles corner edges collision check. For given conditions and tiles, checks corresponding values,
    /// and determins movement of player.
    /// </summary>
    /// <param name="playerPos">Position of player</param>
    /// <param name="diagonalCond">Bool value if player matches diagonal condition</param>
    /// <param name="outside">Condition for if player is outside of tile</param>
    /// <param name="moveCond">Condition for movement</param>
    void CornerEdgesCollision(Vector3 playerPos, bool diagonalCond, bool outside, bool moveCond,bool outsideDiagonalCond = false){
        //outside condition check
        if (outside)   
        {
            if (!outsideDiagonalCond)
            {
                //playerObj.transform.position = lastPos;
                //playerController.moveDir = Vector3.zero;  
                if (Vector3.Distance(playerObj.transform.position, lastPos) <= 0.3f) playerObj.transform.position = lastPos; // stop 
                else playerObj.transform.position = lastMarkedPos;
            }else{
                this.lastPos = playerObj.transform.position; //mark last position before offset
            }
            
        }else{
            //moving inside tile condition
            if (moveCond){  
                if (!diagonalCond){ //crosses diagonal or tile dimensions
                    //playerObj.transform.position = lastPos; // stop
                    //playerController.moveDir = Vector3.zero; 
                    if (Vector3.Distance(playerObj.transform.position, lastPos) <= 0.3f) playerObj.transform.position = lastPos; // stop 
                    else playerObj.transform.position = lastMarkedPos; 
                }else{
                    this.lastPos = playerObj.transform.position; //mark last position before offset
                }
            }
        }
        return;
    }

    /// <summary>
    /// A utility function to calculate area of triangle formed by A(x1, y1) B(x2, y2) and C(x3, y3)
    /// </summary>
    /// <param name="x1">point A.x</param>
    /// <param name="y1">point A.y</param>
    /// <param name="x2">point B.x</param>
    /// <param name="y2">point B.y</param>
    /// <param name="x3">point C.x</param>
    /// <param name="y3">point C.y</param>
    /// <returns>Area formed by three points ( triangle ) </returns>
    private float area(float x1, float y1, float x2, float y2, float x3, float y3)
    {
        return Mathf.Abs((x1 * (y2 - y3) +
                         x2 * (y3 - y1) +
                         x3 * (y1 - y2)) / 2.0f);
    }
 
    /*  */
    /// <summary>
    /// A function to check whether point P(player) liesinside the triangle formed
    ///  by A, B and C
    /// </summary>
    /// <param name="a">Point A(tile pos)</param>
    /// <param name="b">Point B (top end tile pos)</param>
    /// <param name="c">Point C(right most tile pos)</param>
    /// <param name="p">Player position</param>
    /// <returns>True/false if for if player is on given triangle (below diagonal of tile)</returns>
    private bool isInside(int2 a, int2 b, int2 c, Vector3 p)
    {
        /* Calculate area of triangle ABC */
        float A = area(a.x, a.y, b.x, b.y, c.x, c.y);
 
        /* Calculate area of triangle PBC */
        float A1 = area(p.x, p.y, b.x, b.y, c.x, c.y);
 
        /* Calculate area of triangle PAC */
        float A2 = area(a.x, a.y, p.x, p.y, c.x, c.y);
 
        /* Calculate area of triangle PAB */
        float A3 = area(a.x, a.y, b.x, b.y, p.x, p.y);
 
        /* Check if sum of A1, A2 and A3 is same as A */
        return (A == A1 + A2 + A3);
    }

}
