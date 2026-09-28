public enum JudgmentChoice
{
    No = 0,
    Yes = 1
}

public class JudgmentDoor : UnityEngine.MonoBehaviour
{
    public JudgmentChoice choice = JudgmentChoice.No;
}
