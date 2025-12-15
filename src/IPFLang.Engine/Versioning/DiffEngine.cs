using IPFLang.Parser;

namespace IPFLang.Versioning
{
    /// <summary>
    /// Compares two versions of a fee schedule and generates a structured diff
    /// </summary>
    public class DiffEngine
    {
        /// <summary>
        /// Compare two parsed scripts and generate a change report
        /// </summary>
        public ChangeReport Compare(Version fromVersion, ParsedScript fromScript, Version toVersion, ParsedScript toScript)
        {
            var report = new ChangeReport(fromVersion, toVersion);

            // Compare fees
            CompareFees(fromScript.Fees, toScript.Fees, report);

            // Compare inputs
            CompareInputs(fromScript.Inputs, toScript.Inputs, report);

            // Compare groups
            CompareGroups(fromScript.Groups, toScript.Groups, report);

            return report;
        }

        private void CompareFees(IEnumerable<DslFee> oldFees, IEnumerable<DslFee> newFees, ChangeReport report)
        {
            var oldFeeDict = oldFees.ToDictionary(f => f.Name);
            var newFeeDict = newFees.ToDictionary(f => f.Name);

            // Find added fees
            foreach (var fee in newFees)
            {
                if (!oldFeeDict.ContainsKey(fee.Name))
                {
                    report.FeeChanges.Add(new FeeChange(
                        fee.Name,
                        ChangeType.Added,
                        NewDefinition: fee.ToString(),
                        IsBreaking: false // New fees are not breaking
                    ));
                }
            }

            // Find removed fees
            foreach (var fee in oldFees)
            {
                if (!newFeeDict.ContainsKey(fee.Name))
                {
                    report.FeeChanges.Add(new FeeChange(
                        fee.Name,
                        ChangeType.Removed,
                        OldDefinition: fee.ToString(),
                        IsBreaking: !fee.Optional // Removing mandatory fee is breaking
                    ));
                }
            }

            // Find modified fees
            foreach (var feeName in oldFeeDict.Keys.Intersect(newFeeDict.Keys))
            {
                var oldFee = oldFeeDict[feeName];
                var newFee = newFeeDict[feeName];

                if (!AreFeesEqual(oldFee, newFee))
                {
                    var isBreaking = IsFeeChangeBreaking(oldFee, newFee);
                    report.FeeChanges.Add(new FeeChange(
                        feeName,
                        ChangeType.Modified,
                        OldDefinition: oldFee.ToString(),
                        NewDefinition: newFee.ToString(),
                        IsBreaking: isBreaking
                    ));
                }
                else
                {
                    report.FeeChanges.Add(new FeeChange(
                        feeName,
                        ChangeType.Unchanged,
                        OldDefinition: oldFee.ToString(),
                        NewDefinition: newFee.ToString()
                    ));
                }
            }
        }

        private void CompareInputs(IEnumerable<DslInput> oldInputs, IEnumerable<DslInput> newInputs, ChangeReport report)
        {
            var oldInputDict = oldInputs.ToDictionary(i => i.Name);
            var newInputDict = newInputs.ToDictionary(i => i.Name);

            // Find added inputs
            foreach (var input in newInputs)
            {
                if (!oldInputDict.ContainsKey(input.Name))
                {
                    report.InputChanges.Add(new InputChange(
                        input.Name,
                        ChangeType.Added,
                        NewDefinition: input.ToString(),
                        IsBreaking: true // New required inputs are breaking
                    ));
                }
            }

            // Find removed inputs
            foreach (var input in oldInputs)
            {
                if (!newInputDict.ContainsKey(input.Name))
                {
                    report.InputChanges.Add(new InputChange(
                        input.Name,
                        ChangeType.Removed,
                        OldDefinition: input.ToString(),
                        IsBreaking: true // Removing inputs is breaking
                    ));
                }
            }

            // Find modified inputs
            foreach (var inputName in oldInputDict.Keys.Intersect(newInputDict.Keys))
            {
                var oldInput = oldInputDict[inputName];
                var newInput = newInputDict[inputName];

                if (!AreInputsEqual(oldInput, newInput))
                {
                    var isBreaking = IsInputChangeBreaking(oldInput, newInput);
                    report.InputChanges.Add(new InputChange(
                        inputName,
                        ChangeType.Modified,
                        OldDefinition: oldInput.ToString(),
                        NewDefinition: newInput.ToString(),
                        IsBreaking: isBreaking
                    ));
                }
                else
                {
                    report.InputChanges.Add(new InputChange(
                        inputName,
                        ChangeType.Unchanged,
                        OldDefinition: oldInput.ToString(),
                        NewDefinition: newInput.ToString()
                    ));
                }
            }
        }

        private void CompareGroups(IEnumerable<DslGroup> oldGroups, IEnumerable<DslGroup> newGroups, ChangeReport report)
        {
            var oldGroupDict = oldGroups.ToDictionary(g => g.Name);
            var newGroupDict = newGroups.ToDictionary(g => g.Name);

            // Find added groups
            foreach (var group in newGroups)
            {
                if (!oldGroupDict.ContainsKey(group.Name))
                {
                    report.GroupChanges.Add(new GroupChange(
                        group.Name,
                        ChangeType.Added,
                        NewDefinition: group.ToString()
                    ));
                }
            }

            // Find removed groups
            foreach (var group in oldGroups)
            {
                if (!newGroupDict.ContainsKey(group.Name))
                {
                    report.GroupChanges.Add(new GroupChange(
                        group.Name,
                        ChangeType.Removed,
                        OldDefinition: group.ToString()
                    ));
                }
            }

            // Find modified groups
            foreach (var groupName in oldGroupDict.Keys.Intersect(newGroupDict.Keys))
            {
                var oldGroup = oldGroupDict[groupName];
                var newGroup = newGroupDict[groupName];

                if (!AreGroupsEqual(oldGroup, newGroup))
                {
                    report.GroupChanges.Add(new GroupChange(
                        groupName,
                        ChangeType.Modified,
                        OldDefinition: oldGroup.ToString(),
                        NewDefinition: newGroup.ToString()
                    ));
                }
                else
                {
                    report.GroupChanges.Add(new GroupChange(
                        groupName,
                        ChangeType.Unchanged,
                        OldDefinition: oldGroup.ToString(),
                        NewDefinition: newGroup.ToString()
                    ));
                }
            }
        }

        private bool AreFeesEqual(DslFee fee1, DslFee fee2)
        {
            // Compare basic properties
            if (fee1.Optional != fee2.Optional) return false;
            if (fee1.IsPolymorphic != fee2.IsPolymorphic) return false;
            if (fee1.TypeParameter != fee2.TypeParameter) return false;
            if (fee1.ReturnCurrency != fee2.ReturnCurrency) return false;

            // Compare string representation for deep comparison
            // This includes cases and yields
            return fee1.ToString() == fee2.ToString();
        }

        private bool AreInputsEqual(DslInput input1, DslInput input2)
        {
            // Must be same type
            if (input1.GetType() != input2.GetType()) return false;

            // Compare string representation
            return input1.ToString() == input2.ToString();
        }

        private bool AreGroupsEqual(DslGroup group1, DslGroup group2)
        {
            return group1.Text == group2.Text && group1.Weight == group2.Weight;
        }

        private bool IsFeeChangeBreaking(DslFee oldFee, DslFee newFee)
        {
            // Changing optional to mandatory is breaking
            if (!oldFee.Optional && newFee.Optional) return false;
            if (oldFee.Optional && !newFee.Optional) return true;

            // Changing fee structure is potentially breaking
            // We consider any modification to a mandatory fee as breaking
            return !oldFee.Optional;
        }

        private bool IsInputChangeBreaking(DslInput oldInput, DslInput newInput)
        {
            // Type changes are breaking
            if (oldInput.GetType() != newInput.GetType()) return true;

            // For number inputs, narrowing range is breaking
            if (oldInput is DslInputNumber oldNum && newInput is DslInputNumber newNum)
            {
                if (newNum.MinValue > oldNum.MinValue) return true;
                if (newNum.MaxValue < oldNum.MaxValue) return true;
            }

            // For list inputs, removing choices is breaking
            if (oldInput is DslInputList oldList && newInput is DslInputList newList)
            {
                var oldChoices = oldList.Items.Select(i => i.Symbol).ToHashSet();
                var newChoices = newList.Items.Select(i => i.Symbol).ToHashSet();
                if (oldChoices.Except(newChoices).Any()) return true;
            }

            // For date inputs, narrowing range is breaking
            if (oldInput is DslInputDate oldDate && newInput is DslInputDate newDate)
            {
                if (newDate.MinValue > oldDate.MinValue) return true;
                if (newDate.MaxValue < oldDate.MaxValue) return true;
            }

            // Default changes are not breaking
            return false;
        }
    }
}
