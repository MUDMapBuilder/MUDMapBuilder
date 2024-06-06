using System.Text;

namespace MUDMapBuilder
{
	public enum EffectBitType
	{
		None,
		Object,
		Immunity,
		Resistance,
		Vulnerability,
		Weapon
	}

	public enum EffectType
	{
		None = 0,
		Strength = 1,
		Dexterity = 2,
		Intelligence = 3,
		Wisdom = 4,
		Constitution = 5,
		Charisma = 6,
		Class = 7,
		Level = 8,
		Age = 9,
		Height = 10,
		Weight = 11,
		Mana = 12,
		Hit = 13,
		Move = 14,
		Gold = 15,
		Exp = 16,
		Ac = 17,
		HitRoll = 18,
		DamRoll = 19,
		SavingPara = 20,
		SavingRod = 21,
		SavingPetri = 22,
		SavingBreath = 23,
		SavingSpell = 24,
	}

	public class MMBEffect
	{
		public EffectBitType EffectBitType { get; set; }
		public EffectType EffectType { get; set; }
		public int Modifier { get; set; }

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.Append(EffectType.ToString());
			sb.Append(" ");
			if (Modifier > 0)
			{
				sb.Append("+");
			}
			else if (Modifier < 0)
			{
				sb.Append("-");
			}

			sb.Append(Modifier);

			return sb.ToString();
		}
	}
}
