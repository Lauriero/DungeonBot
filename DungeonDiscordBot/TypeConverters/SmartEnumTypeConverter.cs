using Ardalis.SmartEnum;

using Discord;
using Discord.Interactions;

namespace DungeonDiscordBot.TypeConverters;

public class SmartEnumTypeConverter<TEnum, TValue> : TypeConverter<TEnum>
    where TEnum : SmartEnum<TEnum, TValue>
    where TValue : IEquatable<TValue>, IComparable<TValue>
{
    public override ApplicationCommandOptionType GetDiscordType() => ApplicationCommandOptionType.String;

    public override async Task<TypeConverterResult> ReadAsync(IInteractionContext context, IApplicationCommandInteractionDataOption option, IServiceProvider services)
    {
        if (option.Value is not string value) {
            return TypeConverterResult.FromError(
                new ArgumentException("Value is not a string"));
        }

        if (!SmartEnum<TEnum, TValue>.TryFromName(value, out TEnum result)) {
            return TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, 
                $"Value {value} cannot be converted."); 
        }
        
        return TypeConverterResult.FromSuccess(result);
    }

    public override void Write(ApplicationCommandOptionProperties properties, IParameterInfo parameter)
    {
        var names = SmartEnum<TEnum, TValue>.List;
        properties.Choices = names
            .Take(25)
            .Select((e) => 
                new ApplicationCommandOptionChoiceProperties {
                    Name = e.Name,
                    Value = e.Name
                })
            .ToList();
    }
}