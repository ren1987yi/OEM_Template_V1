var lb = Owner.GetAlias("LabelNode") as Label;

Random rnd = new Random();
if (lb != null)
{
    var val = rnd.Next(100);
    lb.Text = val.ToString();
    if(val > 50)
    {

    lb.TextColor = Colors.Red;
    }else
    {
        lb.TextColor = Colors.Blue;
    }
}
return "";