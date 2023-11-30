public interface IState 
{
    void OnStateEnter();
    void OnTick();
    void OnStateExit();

}
