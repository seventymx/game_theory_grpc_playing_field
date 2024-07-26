/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * Author: Steffen70 <steffen@seventy.mx>
 * Creation Date: 2024-07-25
 * 
 * Contributors:
 * - Contributor Name <contributor@example.com>
 */

using Seventy.GameTheory.Model;

namespace Seventy.GameTheory.PlayingField.Extensions;

public static class PlayerActionExtensions
{
    /// <summary>
    /// Converts a <see cref="PlayerAction"/> to an <see cref="OpponentAction"/>.
    /// If the action is null, returns <see cref="OpponentAction.None"/>.
    /// </summary>
    public static OpponentAction ToOpponentAction(this PlayerAction? action) =>
        action switch
        {
            PlayerAction.Cooperate => OpponentAction.Cooperated,
            PlayerAction.Defect => OpponentAction.Defected,
            // Return None if the action is null
            _ => OpponentAction.None
        };
}