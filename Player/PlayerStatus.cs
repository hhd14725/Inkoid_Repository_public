public class PlayerStatus
{
    public float Health;
    public float MaximumHealth;

    public float Ink;
    public float MaximumInk;

    public float HorizontalityMoveSpeed;
    public float VerticalMoveSpeed;
    public float MaximumMoveSpeed;

    public float DashForce;


    public PlayerStatus(
        float health, float maximumHealth,
        float ink, float maximumInk,
        float horizontalityMoveSpeed, float verticalMoveSpeed, float maximumMoveSpeed,
        float dashForce
    )
    {
        Health = health;
        MaximumHealth = maximumHealth;

        Ink = ink;
        MaximumInk = maximumInk;

        HorizontalityMoveSpeed = horizontalityMoveSpeed;
        VerticalMoveSpeed = verticalMoveSpeed;
        MaximumMoveSpeed = maximumMoveSpeed;

        DashForce = dashForce;
    }
}