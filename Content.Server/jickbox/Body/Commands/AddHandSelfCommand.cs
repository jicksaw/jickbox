using System.Linq;
using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Random;

namespace Content.Server.Body.Commands
{
    [AnyCommand]
    sealed class AddHandSelfCommand : IConsoleCommand
    {
        public const string HandPrototype = "LeftHandHuman";

        public string Command => "addhandself";
        public string Description => "Give yourself a hand";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not IPlayerSession player)
            {
                shell.WriteError("This command cannot be run from the server.");
                return;
            }

            if (player.Status != SessionStatus.InGame)
                return;

            if (player.AttachedEntity is not {} playerEntity)
            {
                shell.WriteError("You don't have an entity!");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            EntityUid entity = player.AttachedEntity.Value;

            if (!entityManager.TryGetComponent(entity, out BodyComponent? body) || body.Root == null)
            {
                var random = IoCManager.Resolve<IRobustRandom>();
                var text = $"You have no body{(random.Prob(0.2f) ? " and you must scream." : ".")}";

                shell.WriteLine(text);
                return;
            }

            EntityUid hand = entityManager.SpawnEntity(HandPrototype, entityManager.GetComponent<TransformComponent>(entity).Coordinates);

            if (!entityManager.TryGetComponent(hand, out BodyPartComponent? part))
            {
                shell.WriteLine($"Hand entity {hand} does not have a {nameof(BodyPartComponent)} component.");
                return;
            }

            var bodySystem = entityManager.System<BodySystem>();

            var existingHands = bodySystem.GetBodyChildrenOfType(entity, BodyPartType.Hand, body);
            if (existingHands.Count() > 5)
            {
                shell.WriteLine($"You have enough hands already");
                return;
            }

            var attachAt =  bodySystem.GetBodyChildrenOfType(entity, BodyPartType.Arm, body).FirstOrDefault();
            if (attachAt == default)
                attachAt = bodySystem.GetBodyChildren(entity, body).First();

            var slotId = part.GetHashCode().ToString();

            if (!bodySystem.TryCreatePartSlotAndAttach(attachAt.Id, slotId, hand, attachAt.Component, part))
            {
                shell.WriteError($"Couldn't create a slot with id {slotId} on entity {entityManager.ToPrettyString(entity)}");
                return;
            }

            shell.WriteLine($"Added hand to entity {entityManager.GetComponent<MetaDataComponent>(entity).EntityName}");
        }
    }
}
