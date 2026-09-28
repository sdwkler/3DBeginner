using UnityEngine;

public class GhostSleeper : GhostActor
{
    void Update()
    {
        if (!Visible)
            return;

        if (HoldForStare())
            return;

        BobUpright();
    }

    public override void ResetHome()
    {
        base.ResetHome();
        SetGhostVisible(true);
    }
}
