using UnityEngine;
using Rewired;

[RequireComponent(typeof(Player2D))]
public class PlayerInput : MonoBehaviour
{

    Player rewiredPlayer;

    Player2D player;
    Vector2 directionalInput;

    void Awake()
    {
        rewiredPlayer = ReInput.players.GetPlayer(RewiredConsts.Player.PLAYER0);
    }

    void Start()
    {
        player = GetComponent<Player2D>();
    }

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        directionalInput.x = rewiredPlayer.GetAxis(RewiredConsts.Action.MOVE_HORIZONTAL);

        player.SetDirectionalInput(directionalInput);

        if (rewiredPlayer.GetButtonDown(RewiredConsts.Action.JUMP))
        {
            player.OnJumpInputDown();
        }

        if (rewiredPlayer.GetButtonUp(RewiredConsts.Action.JUMP))
        {
            player.OnJumpInputUp();
        }

        if (rewiredPlayer.GetButtonDown(RewiredConsts.Action.SOUND))
        {
            player.OnHomingInput();
        }

        if (rewiredPlayer.GetButtonDown(RewiredConsts.Action.TERRIFY))
        {
            Debug.Log("TERRIFY!");
        }
    }

}