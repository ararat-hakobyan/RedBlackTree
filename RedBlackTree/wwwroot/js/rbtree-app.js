'use strict';

const RBTreeApp = (() => {
    const AUTOPLAY_INTERVAL_MS = 500;
    const TRANSITION_MS = 1500;
    const FOCUS_SCALE = 0.8;
    const FOCUS_DURATION_MS = 750;
    const RETURN_DURATION_MS = 500;

    let payload = null;
    let svg = null;
    let view = { width: 0, height: 0 };

    const isRotationPair = (a, b) =>
        (a.Action === 'BeforeRotateLeft' && b.Action === 'AfterRotateLeft') ||
        (a.Action === 'BeforeRotateRight' && b.Action === 'AfterRotateRight');

    const isTransplantPair = (a, b) =>
        (a.Action === 'BeforeTransplant' && b.Action === 'AfterTransplant') ||
        (a.Action === 'BeforeReplaceWithSuccessor' && b.Action === 'AfterReplaceWithSuccessor');

    function relationships(treeState) {
        const result = [];

        (function walk(node) {
            if (!node || node.Value === null) {
                return;
            }

            ['Left', 'Right'].forEach(side => {
                const child = node[side];

                if (child && child.Value !== null) {
                    result.push({ sourceId: node.NodeId, targetId: child.NodeId });
                    walk(child);
                }
            });
        })(treeState);

        return result;
    }

    function indexById(root) {
        const map = {};

        root.descendants().forEach(node => {
            map[node.data.NodeId] = {
                x: node.x,
                y: node.y,
                value: node.data.Value,
                color: node.data.Color
            };
        });

        return map;
    }

    function unify(beforeMap, afterMap) {
        const ids = new Set(Object.keys(beforeMap).concat(Object.keys(afterMap)));

        return Array.from(ids).map(id => {
            const before = beforeMap[id];
            const after = afterMap[id];
            const anchor = before || after;

            return {
                id: id,
                value: anchor.value,
                color: anchor.color,
                from: { x: anchor.x, y: anchor.y },
                to: after ? { x: after.x, y: after.y } : { x: anchor.x, y: anchor.y },
                x: anchor.x,
                y: anchor.y,
                inBefore: Boolean(before),
                inAfter: Boolean(after),
                opacity: before ? 1 : 0
            };
        });
    }

    function measureView() {
        const container = document.getElementById('treeContainer');
        const root = document.documentElement;

        container.style.height = Math.max(300, window.innerHeight - container.getBoundingClientRect().top - 8) + 'px';

        const overflow = root.scrollHeight - root.clientHeight;

        if (overflow > 0) {
            container.style.height = Math.max(300, container.clientHeight - overflow) + 'px';
        }

        view = { width: container.clientWidth, height: container.clientHeight };

        return container;
    }

    function renderStaticTree(treeData, mode) {
        if (!treeData) {
            return;
        }

        svg.selectAll('*').remove();

        const root = RBTreeRender.layout(treeData);
        const items = RBTreeRender.nodesOf(root);
        const group = svg.append('g');

        RBTreeRender.drawLinks(group, RBTreeRender.linksOf(root), null);
        RBTreeRender.drawNodes(group, items, { inlineStyles: false });

        const zoom = RBTreeRender.createZoom(svg, group);
        const threshold = payload.AutoFocusThreshold;
        const focusValue = payload.FocusValue;

        if (mode === 'restore') {
            if (payload.Count < threshold) {
                RBTreeRender.centerHorizontally(group, items, view.width);
                return;
            }

            if (!focusValue) {
                return;
            }

            const target = items.find(item => item.value === focusValue);

            if (target) {
                panTo(zoom, target, 1, 3, FOCUS_DURATION_MS, () => pulseFocused(focusValue));
            }

            return;
        }

        RBTreeRender.centerHorizontally(group, items, view.width);

        if (payload.IsSearchHighlighted && payload.Count <= threshold) {
            pulseFocused(focusValue);
            return;
        }

        if (payload.Count > threshold && focusValue) {
            const matches = items.filter(item => item.value === focusValue);
            const target = matches[matches.length - 1];

            if (target) {
                panTo(zoom, target, 1, 2, FOCUS_DURATION_MS, () => pulseFocused(focusValue));
            }
        }
    }

    function panTo(zoom, point, scale, verticalDivisor, duration, onEnd) {
        const transition = RBTreeRender.focusOn(svg, zoom, point, view, {
            scale: scale,
            verticalDivisor: verticalDivisor,
            duration: duration
        });

        if (transition && onEnd) {
            transition.on('end', onEnd);
        }
    }

    function pulseFocused(focusValue) {
        if (!payload.IsSearchHighlighted || !focusValue) {
            return;
        }

        const selection = d3.selectAll('.node').filter(d => d.value === focusValue);

        if (!selection.empty()) {
            RBTreeRender.pulse(selection);
        }
    }

    function createPlayer(steps, initialTree) {
        const pageButtons = Array.from(document.querySelectorAll('button'));

        let stepIndex = 0;
        let isPaused = true;
        let autoplayTimer = null;
        let pendingTimer = null;
        let animationInProgress = false;
        let isRotationSequence = false;

        const panel = buildPanel(steps.length);
        document.body.appendChild(panel);

        const counter = panel.querySelector('#stepCounter');
        const description = panel.querySelector('#stepDescription');
        const prevButton = panel.querySelector('#prevStepBtn');
        const nextButton = panel.querySelector('#nextStepBtn');
        const pauseButton = panel.querySelector('#pauseBtn');
        const closeButton = panel.querySelector('#closeBtn');

        pageButtons.forEach(button => {
            button.disabled = true;
        });

        function buildPanel(totalSteps) {
            const element = document.createElement('div');
            element.id = 'animationStatus';
            element.className = 'step-panel';
            element.innerHTML =
                '<div class="step-panel__counter">Step <span id="stepCounter">1</span> of ' + totalSteps + '</div>' +
                '<div id="stepDescription" class="step-panel__description"></div>' +
                '<div class="step-panel__actions">' +
                '<button id="prevStepBtn" class="modern-btn">&#x25C0; Previous</button>' +
                '<button id="pauseBtn" class="modern-btn">Play</button>' +
                '<button id="nextStepBtn" class="modern-btn">Next &#x25B6;</button>' +
                '<button id="closeBtn" class="modern-btn modern-btn--close">Close</button>' +
                '</div>';
            return element;
        }

        function updatePanel() {
            if (stepIndex >= steps.length) {
                stepIndex = steps.length - 1;
            }

            counter.textContent = String(stepIndex + 1);
            description.textContent = RBTreeSteps.describe(steps[stepIndex]);
        }

        function startAutoplay() {
            stopAutoplay();
            autoplayTimer = setInterval(() => {
                if (!isPaused) {
                    goNext();
                }
            }, AUTOPLAY_INTERVAL_MS);
        }

        function stopAutoplay() {
            if (autoplayTimer) {
                clearInterval(autoplayTimer);
                autoplayTimer = null;
            }
        }

        function scheduleNext(delay) {
            if (isPaused) {
                return;
            }

            pendingTimer = setTimeout(goNext, delay);
        }

        function goNext() {
            if (animationInProgress) {
                return;
            }

            if (stepIndex >= steps.length - 1) {
                stopAutoplay();
                isPaused = true;
                pauseButton.textContent = 'Play';
                return;
            }

            const current = steps[stepIndex];
            const next = steps[stepIndex + 1];

            if (isRotationPair(current, next)) {
                stepIndex += 1;
                renderTransition(current, next, current.NodeId, current.NodeId, true);
            } else if (isTransplantPair(current, next)) {
                stepIndex += 1;
                renderTransition(current, next, current.NodeId, next.NodeId, false);
            } else {
                stepIndex += 1;
                renderStep(steps[stepIndex]);
            }

            updatePanel();
        }

        function goPrevious() {
            stopAutoplay();

            if (isRotationSequence || animationInProgress || stepIndex === 0) {
                return;
            }

            stepIndex -= 1;
            renderStep(steps[stepIndex]);
            updatePanel();
        }

        function renderStep(step) {
            if (!step || !step.TreeState) {
                return;
            }

            const previous = steps[stepIndex - 1];

            if (previous && isRotationPair(previous, step)) {
                isRotationSequence = true;
                renderTransition(previous, step, previous.NodeId, previous.NodeId, true);
                return;
            }

            svg.selectAll('*').remove();

            const root = RBTreeRender.layout(step.TreeState);
            const items = RBTreeRender.nodesOf(root);
            const group = svg.append('g');
            const linksGroup = group.append('g').attr('class', 'links-group');
            const nodesGroup = group.append('g').attr('class', 'nodes-group');

            const isRotation = step.Action.indexOf('Rotate') !== -1;

            RBTreeRender.drawLinks(
                linksGroup,
                RBTreeRender.linksOf(root),
                (sourceId, targetId) =>
                    isRotation && (sourceId === step.NodeId || targetId === step.NodeId));

            RBTreeRender.drawNodes(nodesGroup, items, {
                isFocused: id => id === step.NodeId,
                fillOverride: item => {
                    if (item.id !== step.NodeId) {
                        return null;
                    }

                    if (step.Action === 'AddNode') {
                        return '#ff0000';
                    }

                    if (step.Action === 'ColorChange') {
                        return step.Color === 'Red' ? '#ff3333' : '#000000';
                    }

                    return null;
                }
            });

            RBTreeRender.centerHorizontally(group, items, view.width);

            const zoom = RBTreeRender.createZoom(svg, group);

            if (payload.Count > payload.AutoFocusThreshold && step.NodeId) {
                const target = items.find(item => item.id === step.NodeId);

                if (target) {
                    panTo(zoom, target, FOCUS_SCALE, 4, FOCUS_DURATION_MS);
                }
            }
        }

        function renderTransition(currentStep, nextStep, beforeFocusId, afterFocusId, isRotation) {
            animationInProgress = true;
            svg.selectAll('*').remove();

            const beforeRoot = RBTreeRender.layout(currentStep.TreeState);
            const afterRoot = RBTreeRender.layout(nextStep.TreeState);
            const beforeMap = indexById(beforeRoot);
            const afterMap = indexById(afterRoot);
            const nodes = unify(beforeMap, afterMap);

            const beforeLinks = relationships(currentStep.TreeState);
            const afterLinks = relationships(nextStep.TreeState);

            const group = svg.append('g');
            const linksGroup = group.append('g').attr('class', 'links-group');
            const nodesGroup = group.append('g').attr('class', 'nodes-group');

            const allX = beforeRoot.descendants().concat(afterRoot.descendants()).map(d => d.x);
            const offsetX = (view.width - (Math.max(...allX) - Math.min(...allX))) / 2 - Math.min(...allX);
            group.attr('transform', `translate(${offsetX}, ${RBTreeRender.TOP_MARGIN})`);

            const positions = {};
            nodes.forEach(node => {
                positions[node.id] = node;
            });

            const isFocused = id => id === currentStep.NodeId || id === nextStep.NodeId;

            function paintLinks(progress) {
                const source = progress < 0.5 ? beforeLinks : afterLinks;
                const focusId = progress < 0.5 ? beforeFocusId : afterFocusId;

                const links = source
                    .filter(link => positions[link.sourceId] && positions[link.targetId])
                    .map(link => {
                        const from = positions[link.sourceId];
                        const to = positions[link.targetId];

                        return {
                            source: { x: from.x, y: from.y },
                            target: { x: to.x, y: to.y },
                            sourceId: link.sourceId,
                            targetId: link.targetId,
                            opacity: from.opacity * to.opacity
                        };
                    });

                RBTreeRender.drawLinks(
                    linksGroup,
                    links,
                    (sourceId, targetId) => sourceId === focusId || targetId === focusId);
            }

            const nodeSelection = RBTreeRender.drawNodes(nodesGroup, nodes, { isFocused: isFocused });
            const zoom = RBTreeRender.createZoom(svg, group);
            const largeTree = payload.Count > payload.AutoFocusThreshold;

            if (largeTree) {
                const entryTarget = positions[currentStep.NodeId] || positions[nextStep.NodeId];

                if (entryTarget) {
                    RBTreeRender.focusOn(svg, zoom, entryTarget, view, {
                        scale: FOCUS_SCALE,
                        verticalDivisor: 3,
                        duration: 0
                    });
                }
            }

            const startedAt = Date.now();

            function frame() {
                const progress = Math.min((Date.now() - startedAt) / TRANSITION_MS, 1);
                const eased = d3.easeCubicInOut(progress);

                nodes.forEach(node => {
                    if (node.inBefore && node.inAfter) {
                        node.opacity = 1;
                        node.x = node.from.x + (node.to.x - node.from.x) * eased;
                        node.y = node.from.y + (node.to.y - node.from.y) * eased;
                    } else if (node.inBefore) {
                        node.opacity = 1 - eased;
                    } else {
                        node.opacity = eased;
                    }
                });

                nodeSelection
                    .attr('transform', d => `translate(${d.x},${d.y})`)
                    .style('opacity', d => d.opacity);

                paintLinks(eased);

                if (progress < 1) {
                    requestAnimationFrame(frame);
                    return;
                }

                animationInProgress = false;

                if (largeTree && nextStep.NodeId && positions[nextStep.NodeId]) {
                    RBTreeRender.focusOn(svg, zoom, positions[nextStep.NodeId], view, {
                        scale: FOCUS_SCALE,
                        verticalDivisor: 4,
                        duration: RETURN_DURATION_MS
                    });
                }

                if (isRotation) {
                    isRotationSequence = false;

                    const upcoming = steps[stepIndex];

                    if (!isPaused && upcoming && upcoming.Action.indexOf('Rotate') !== -1) {
                        goNext();
                        return;
                    }
                }

                scheduleNext(AUTOPLAY_INTERVAL_MS);
            }

            requestAnimationFrame(frame);
        }

        function onKeyDown(event) {
            if (event.key === 'ArrowRight') {
                goNext();
            } else if (event.key === 'ArrowLeft') {
                goPrevious();
            } else if (event.key === ' ') {
                pauseButton.click();
            }
        }

        function dispose() {
            stopAutoplay();

            if (pendingTimer) {
                clearTimeout(pendingTimer);
                pendingTimer = null;
            }

            document.removeEventListener('keydown', onKeyDown);

            if (panel.parentNode) {
                panel.parentNode.removeChild(panel);
            }

            pageButtons.forEach(button => {
                button.disabled = false;
            });
        }

        prevButton.addEventListener('click', () => {
            if (isPaused) {
                goPrevious();
            }
        });

        nextButton.addEventListener('click', () => {
            if (isPaused || autoplayTimer) {
                goNext();
            }
        });

        pauseButton.addEventListener('click', () => {
            isPaused = !isPaused;
            pauseButton.textContent = isPaused ? 'Play' : 'Pause';

            if (isPaused) {
                stopAutoplay();
            } else {
                startAutoplay();
            }
        });

        closeButton.addEventListener('click', () => {
            dispose();
            renderStaticTree(initialTree, 'restore');
        });

        document.addEventListener('keydown', onKeyDown);

        renderStep(steps[0]);
        updatePanel();
    }

    function playSteps() {
        if (!payload.Steps || payload.Steps.length === 0) {
            window.alert('There are no steps to show.');
            return;
        }

        createPlayer(payload.Steps, payload.Root);
    }

    function wireControls() {
        const fileInput = document.getElementById('fileUpload');
        const importForm = document.getElementById('importForm');
        const functionInput = document.getElementById('funcInput');
        const exportButton = document.getElementById('exportButton');
        const playButton = document.getElementById('playStepsButton');

        if (fileInput && importForm && functionInput) {
            fileInput.addEventListener('change', () => {
                functionInput.value = 'import';
                importForm.submit();
            });
        }

        if (exportButton && functionInput) {
            exportButton.addEventListener('click', () => {
                functionInput.value = 'export';
            });
        }

        if (playButton) {
            playButton.addEventListener('click', playSteps);
        }
    }

    function init() {
        const payloadElement = document.getElementById('treePayload');

        if (!payloadElement) {
            return;
        }

        payload = JSON.parse(payloadElement.textContent);

        const container = measureView();

        svg = d3.select('#treeSVG')
            .attr('width', view.width)
            .attr('height', view.height);

        wireControls();

        window.addEventListener('resize', () => {
            measureView();
            svg.attr('width', view.width).attr('height', view.height);
            svg.selectAll('*').remove();

            if (payload.Root) {
                renderStaticTree(payload.Root, 'initial');
            }
        });

        if (container && payload.Root) {
            renderStaticTree(payload.Root, 'initial');
        }
    }

    return { init: init };
})();

document.addEventListener('DOMContentLoaded', RBTreeApp.init);
