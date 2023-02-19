namespace MilkWang1;

public class SC2Resource
{
    public int mineral;
    public int vespine;
    public int food;

    public bool TryPay(int mineralCost, int vespineCost, int foodCost)
    {
        if (mineral < mineralCost && mineralCost != 0)
            return false;
        if (vespine < vespineCost && vespineCost != 0)
            return false;
        if (food < foodCost && foodCost != 0)
            return false;
        mineral -= mineralCost;
        vespine -= vespineCost;
        food -= foodCost;
        return true;
    }
}
