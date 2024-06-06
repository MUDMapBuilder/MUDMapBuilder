using System;

namespace MUDMapBuilder.Import.Diku
{
	[Flags]
	public enum OldResistanceFlags
	{
		None = 0,
		Summon = 1 << 0,
		Charm = 1 << 1,
		Magic = 1 << 2,
		Weapon = 1 << 3,
		Bash = 1 << 4,
		Pierce = 1 << 5,
		Slash = 1 << 6,
		Fire = 1 << 7,
		Cold = 1 << 8,
		Lightning = 1 << 9,
		Acid = 1 << 10,
		Poison = 1 << 11,
		Negative = 1 << 12,
		Holy = 1 << 13,
		Energy = 1 << 14,
		Mental = 1 << 15,
		Disease = 1 << 16,
		Drowning = 1 << 17,
		Light = 1 << 18,
		Sound = 1 << 19,
		Wood = 1 << 23,
		Silver = 1 << 24,
		Iron = 1 << 25,
	}

	[Flags]
	public enum OldAffectedByFlags
	{
		None = 0,
		Blindness = 1 << 0,
		Invisible = 1 << 1,
		DetectEvil = 1 << 2,
		DetectInvis = 1 << 3,
		DetectMagic = 1 << 4,
		DetectHidden = 1 << 5,
		DetectGood = 1 << 6,
		Sanctuary = 1 << 7,
		FaerieFire = 1 << 8,
		Infrared = 1 << 9,
		Curse = 1 << 10,
		Poison = 1 << 12,
		ProtectEvil = 1 << 13,
		ProtectGood = 1 << 14,
		Sneak = 1 << 15,
		Hide = 1 << 16,
		Sleep = 1 << 17,
		Charm = 1 << 18,
		Flying = 1 << 19,
		PassDoor = 1 << 20,
		Haste = 1 << 21,
		Calm = 1 << 22,
		Plague = 1 << 23,
		Weaken = 1 << 24,
		DarkVision = 1 << 25,
		Berserk = 1 << 26,
		Swim = 1 << 27,
		Regeneration = 1 << 28,
		Slow = 1 << 29,
	}
}
