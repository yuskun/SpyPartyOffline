using UnityEngine;

public class FunctionCard : Card
{
  public int Step = 0;
  public int totalSteps = 0;
  public virtual void Execute(CardUseParameters parameters)
  {

  }
  public virtual void SetParameter()
  {

  }
  public virtual void Reset()
  {
    Step = 0;
  }
  

}
