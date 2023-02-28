namespace MilkWangBase;

public class SC2Resource
{
    public int mineral;
    public int vespene;
    public int food;

    public bool ResourceEnough(int mineralCost, int vespeneCost, int foodCost)
    {
        if (mineral < mineralCost && mineralCost != 0)
            return false;
        if (vespene < vespeneCost && vespeneCost != 0)
            return false;
        if (food < foodCost && foodCost != 0)
            return false;
        return true;
    }

    public bool TryPay(int mineralCost, int vespeneCost, int foodCost)
    {
        if (mineral < mineralCost && mineralCost != 0)
            return false;
        if (vespene < vespeneCost && vespeneCost != 0)
            return false;
        if (food < foodCost && foodCost != 0)
            return false;
        mineral -= mineralCost;
        vespene -= vespeneCost;
        food -= foodCost;
        return true;
    }
}
