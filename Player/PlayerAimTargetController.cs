using UnityEngine;

public class PlayerAimTargetController : MonoBehaviour
{
   private PlayerHandler playerHandler;
   private PlayerAction  playerAction;

   private Vector2 MouseInput;

   private void Awake()
   {
       playerHandler = GetComponent<PlayerHandler>();
       playerAction = playerHandler.playerAction;
   }
    
}
