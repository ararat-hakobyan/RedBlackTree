'use strict';

const RBTreeSteps = (() => {
    const colorName = color => (color === 'Red' ? 'red' : 'black');

    const repairStep = step => `Repairing the tree around ${step.NodeValue}.`;

    const replaceWithSuccessor = step =>
        `The node is replaced by the smallest value larger than it, which is ${step.NodeValue}.`;

    const DESCRIPTIONS = {
        AddNode: s => `Inserting a new node with value ${s.NodeValue}.`,

        BeforeRotateLeft: s => `Preparing a left rotation around ${s.NodeValue}.`,
        AfterRotateLeft: () => 'Left rotation finished.',
        BeforeRotateRight: s => `Preparing a right rotation around ${s.NodeValue}.`,
        AfterRotateRight: () => 'Right rotation finished.',

        ColorChange: s => `${s.NodeValue} becomes ${colorName(s.Color)}.`,

        BeginDelete: s => `Starting to delete ${s.NodeValue}.`,
        DeleteCaseLeftNil: s =>
            `The left child is NIL, so ${s.NodeValue} is replaced by its right child.`,
        DeleteCaseRightNil: s =>
            `The right child is NIL, so ${s.NodeValue} is replaced by its left child.`,
        FindSuccessor: s =>
            `Found the smallest value in the right subtree. Its value is ${s.NodeValue}.`,
        SuccessorIsDirectChild: s => `The successor ${s.NodeValue} is a direct child.`,
        BeforeTransplantSuccessor: s => `Moving the successor ${s.NodeValue} out of its old place.`,
        AfterTransplantSuccessor: s => `The successor ${s.NodeValue} has been moved.`,
        BeforeReplaceWithSuccessor: replaceWithSuccessor,
        AfterReplaceWithSuccessor: replaceWithSuccessor,

        BeforeTransplant: s => `${s.NodeValue} is about to be replaced.`,
        AfterTransplant: s =>
            (s.NodeValue ? `${s.NodeValue} has taken over that place.` : 'That place is now empty (NIL).'),

        FindMinimumStart: () =>
            'This node has two children, so we look for the smallest value in its right subtree.',
        FindMinimumStep: s => `Moving left from ${s.NodeValue}.`,
        FindMaximumStart: () => 'Looking for the largest value.',
        FindMaximumStep: s => `Moving right from ${s.NodeValue}.`,
        FindMaximumComplete: s => `The largest value is ${s.NodeValue}.`,

        BeginFixDelete: s => `Repairing the tree, starting at ${s.NodeValue}.`,
        FixDeleteCase1: repairStep,
        FixDeleteCase2: repairStep,
        FixDeleteCase3: repairStep,
        FixDeleteCase4: repairStep,
        FixDeleteCase5: repairStep,
        FixDeleteCase6: repairStep,
        FixDeleteCase7: repairStep,
        FixDeleteCase8: repairStep,
        DeleteComplete: s => `${s.NodeValue} has been deleted.`,

        BeginSearch: s => `Searching for ${s.NodeValue}.`,
        SearchStep: s => `Comparing with ${s.NodeValue}.`,
        SearchGoLeft: s => `Smaller than ${s.NodeValue}, so go left.`,
        SearchGoRight: s => `Larger than ${s.NodeValue}, so go right.`,
        SearchFound: () => 'Found it.',
        SearchNotFound: () => 'The value is not in the tree.'
    };

    const fallback = step => step.Action;

    return {
        describe(step) {
            return (DESCRIPTIONS[step.Action] || fallback)(step);
        }
    };
})();
