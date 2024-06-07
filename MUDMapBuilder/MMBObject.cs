using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace MUDMapBuilder
{
	public enum AttackType
	{
		Hit,
		Slice,
		Stab,
		Slash,
		Whip,
		Claw,
		Hack,
		Blast,
		Pound,
		Crush,
		Grep,
		Bite,
		Pierce,
		Suction,
		Beating,
		Charge,
		Slap,
		Punch,
		Cleave,
		Scratch,
		Peck,
		Chop,
		Sting,
		Smash,
		Chomp,
		Thrust,
		Slime,
		Shock
	}

	public enum ItemType
	{
		Light = 1,
		Scroll = 2,
		Wand = 3,
		Staff = 4,
		Weapon = 5,
		Furniture = 6,
		Free = 7,
		Treasure = 8,
		Armor = 9,
		Potion = 10,
		Worn = 11,
		Other = 12,
		Trash = 13,
		Free2 = 14,
		Container = 15,
		Note = 16,
		DrinkContainer = 17,
		Key = 18,
		Food = 19,
		Money = 20,
		Boat = 21,
		Fountain = 23,
		Pill,
	}

	[Flags]
	public enum WearFlags
	{
		Take = 1 << 0,
		Finger = 1 << 1,
		Neck = 1 << 2,
		Body = 1 << 3,
		Head = 1 << 4,
		Legs = 1 << 5,
		Feet = 1 << 6,
		Hands = 1 << 7,
		Arms = 1 << 8,
		Shield = 1 << 9,
		About = 1 << 10,
		Waist = 1 << 11,
		Wrist = 1 << 12,
		Wield = 1 << 13,
		Hold = 1 << 14,
	}

	public enum WeaponType
	{
		Exotic,
		Sword,
		Mace,
		Dagger,
		Axe,
		Staff,
		Flail,
		Whip,
		Polearm
	}

	[Flags]
	public enum ItemExtraFlags
	{
		None = 0,
		Glow = 1 << 0,
		Humming = 1 << 1,
		NoRent = 1 << 2,
		NoDonate = 1 << 3,
		NoInvis = 1 << 4,
		Invisible = 1 << 5,
		Magic = 1 << 6,
		NoDrop = 1 << 7,
		Bless = 1 << 8,
		AntiGood = 1 << 9,
		AntiEvil = 1 << 10,
		AntiNeutral = 1 << 11,
		MagicUser = 1 << 12,
		Cleric = 1 << 13,
		Thief = 1 << 14,
		Warrior = 1 << 15,
		NoSell = 1 << 16,
		Quest = 1 << 17,
	}

	public enum AffectedByFlags
	{
		Infrared,
		Flying,
		DetectIllusion,
		DetectEvil,
		DetectGood,
		DetectHidden,
		DetectInvis,
		DetectMagic,
		Haste,
		Sanctuary,
		Hide,
		PassDoor,
		DarkVision,
		AcuteVision,
		Sneak,
		ProtectionEvil,
		ProtectionGood,
		Plague,
		Berserk,
		Invisible,
		Swim,
		Slow,
		FaerieFire,
		Regeneration,
		Weaken,
		Blind,
		Poison,
		Curse,
		Camouflage,
		Charm,
		Sleep,
		Calm,

		ProtectEvil = ProtectionEvil,
		ProtectGood = ProtectionGood,
		Blindness = Blind,
	}

	public enum LiquidType
	{
		Water,
		Beer,
		RedWine,
		Ale,
		DarkAle,
		Whisky,
		Lemonade,
		Firebreather,
		LocalSpecialty,
		SlimeMoldJuice,
		Milk,
		Tea,
		Coffee,
		Blood,
		SaltWater,
		Coke,
		RootBeer,
		ElvishWine,
		WhiteWine,
		Champagne,
		Mead,
		RoseWine,
		BenidictineWine,
		Vodka,
		CranberryJuice,
		OrangeJuice,
		Absinthe,
		Brandy,
		Aquavit,
		Schnapps,
		Icewine,
		Amontillado,
		Sherry,
		Framboise,
		Rum,
		Cordial
	}

	public enum Skill
	{
		// Spells
		Reserved,
		AcidBlast,
		Armor,
		Bless,
		Blindness,
		BurningHands,
		CallLightning,
		Calm,
		Cancellation,
		CauseCritical,
		CauseLight,
		CauseSerious,
		ChainLightning,
		ChangeSex,
		CharmPerson,
		ChillTouch,
		ColourSpray,
		ContinualLight,
		ControlWeather,
		CreateFood,
		CreateRose,
		CreateSpring,
		CreateWater,
		CureBlindness,
		CureCritical,
		CureDisease,
		CureLight,
		CurePoison,
		CureSerious,
		Curse,
		Demonfire,
		DetectEvil,
		DetectGood,
		DetectHidden,
		DetectInvis,
		DetectMagic,
		DetectPoison,
		DispelEvil,
		DispelGood,
		DispelMagic,
		Earthquake,
		EnchantArmor,
		EnchantWeapon,
		EnergyDrain,
		FaerieFire,
		FaerieFog,
		Farsight,
		Fireball,
		Fireproof,
		Flamestrike,
		Fly,
		FloatingDisc,
		Frenzy,
		Gate,
		GiantStrength,
		Harm,
		Haste,
		Heal,
		HeatMetal,
		HolyWord,
		Identify,
		Infravision,
		Invisibility,
		KnowAlignment,
		LightningBolt,
		LocateObject,
		MagicMissile,
		MassHealing,
		MassInvis,
		Nexus,
		PassDoor,
		Plague,
		Poison,
		Portal,
		ProtectionEvil,
		ProtectionGood,
		RayOfTruth,
		Recharge,
		Refresh,
		RemoveCurse,
		Sanctuary,
		Shield,
		ShockingGrasp,
		Sleep,
		Slow,
		StoneSkin,
		Summon,
		Teleport,
		Ventriloquate,
		Weaken,
		WordOfRecall,

		// Dragon breath
		AcidBreath,
		FireBreath,
		FrostBreath,
		GasBreath,
		LightningBreath,

		// Special
		GeneralPurpose,
		HighExplosive,

		// Combat & weapon
		Axe,
		Dagger,
		Flail,
		Mace,
		Polearm,
		ShieldBlock,
		Spear,
		Sword,
		Whip,
		Backstab,
		Bash,
		Berserk,
		DirtKicking,
		Disarm,
		Dodge,
		EnhancedDamage,
		Envenom,
		HandToHand,
		Kick,
		Parry,
		Rescue,
		Trip,
		SecondAttack,
		ThirdAttack,

		// Non-combat
		FastHealing,
		Haggle,
		Hide,
		Lore,
		Meditation,
		Peek,
		PickLock,
		Sneak,
		Steal,
		Scrolls,
		Staves,
		Wands,
		Recall
	}

	public enum ResistanceFlags
	{
		Disease,
		Poison,
		Fire,
		Charm,
		Bash,
		Pierce,
		Cold,
		Light,
		Lightning,
		Drowning,
		Summon,
		Magic,
		Weapon,
		Mental,
		Negative,
		Holy,
		Slash,
		Acid,
		Energy,
		Iron,
		Silver,
		Sound,
		Wood,
	}

	public class MMBObject
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string ShortDescription { get; set; }
		public string Description { get; set; }
		public ItemType ItemType { get; set; }
		public WearFlags WearFlags { get; set; }
		public ItemExtraFlags ExtraFlags { get; set; }
		public HashSet<AffectedByFlags> AffectedByFlags { get; set; } = new HashSet<AffectedByFlags>();

		public int Value1 { get; set; }
		public int Value2 { get; set; }
		public int Value3 { get; set; }
		public int Value4 { get; set; }
		public int Value5 { get; set; }
		public string S1 { get; set; }
		public string S2 { get; set; }
		public string S3 { get; set; }
		public string S4 { get; set; }
		public int Level { get; set; }
		public int Weight { get; set; }
		public int Cost { get; set; }
		public int Condition { get; set; }
		public string ExtraKeyword { get; set; }
		public string ExtraDescription { get; set; }
		public List<MMBEffect> Effects { get; set; } = new List<MMBEffect>();

		public string BuildStringValue()
		{
			switch (ItemType)
			{
				case ItemType.Weapon:
					return $"{Value2}d{Value3}";

				case ItemType.Armor:
					return Value1.ToString();
			}

			return string.Empty;
		}

		public string BuildEffectsValue()
		{
			return string.Join(", ", (from ef in Effects select ef.ToString()).ToArray());
		}
	}
}