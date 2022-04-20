using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.InlineRT;
using MonoMod.Utils;
using System;

namespace MonoMod
{
    class MonoModRules
    {
        static MonoModRules()
        {
            // Fix LoadLevel.DoHubModifications
            var doHubMods = MonoModRule.Modder.FindType("LoadLevel").Resolve()
                .FindMethod("DoHubModifications").Body;
            var instrs = doHubMods.Instructions;
            var ilprocessor = doHubMods.GetILProcessor();

            for (int i = 0; i < instrs.Count; i++)
            {
                var instr = instrs[i];

                // Every time the method sets level.hubShortcutWon on a level,
                // we first check if level is null, and if so skip the access.
                // This fixes a crash involving hub areas with certain names
                // not being present.
                if (instr.Operand is FieldDefinition f && f.Name == "hubShortcutWon")
                {
                    // The instruction at ifTrue pushed the value to be assigned.
                    // Both branch instructions pop the stack.
                    var ifTrue = instrs[i - 1];
                    var ifFalse = instrs[i + 1];
                    instrs.Insert(i - 1, ilprocessor.Create(OpCodes.Dup));
                    instrs.Insert(i, ilprocessor.Create(OpCodes.Brtrue_S, ifTrue));
                    instrs.Insert(i + 1, ilprocessor.Create(OpCodes.Brfalse_S, ifFalse));
                    i += 4;
                }
                // If a hub level is not in another hub level, we forgo the usual rules
                // about levels that aren't connected to anything being unlocked.
                // In game, this only occurs in the intro, which the game fixes by checking
                // area name.
                else if (instr.Operand is MethodDefinition m && m.Name == "GetExitBlock")
                {
                    var jumpTarget = instrs[i + 4].Operand as Instruction;
                    instrs[i + 4].Operand = instrs[instrs.IndexOf(jumpTarget) + 8];
                    break;
                }
            }
        }
    }
}
