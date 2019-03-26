using UnityEngine;

public class VisibleWhenAttribute : PropertyAttribute
{
	public readonly string[] ConditionMembers;
	public readonly char Operator;

	public VisibleWhenAttribute(params string[] conditionMembers) :
		this('&', conditionMembers)
	{
	}

	public VisibleWhenAttribute(char operand, params string[] conditionMembers)
	{
		this.ConditionMembers = conditionMembers;
		this.Operator = operand;
	}
}