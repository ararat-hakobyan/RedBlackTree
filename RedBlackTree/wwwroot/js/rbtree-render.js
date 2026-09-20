'use strict';

const RBTreeRender = (() => {
    const NODE_SIZE = [100, 150];
    const SINGLE_CHILD_OFFSET = 50;
    const TOP_MARGIN = 50;

    const RADIUS = 24;
    const RADIUS_FOCUSED = 26;
    const RADIUS_GLOW = 30;

    const FILL_RED = '#ff0000';
    const FILL_BLACK = '#000000';
    const FILL_PULSE = 'darkred';
    const STROKE_DEFAULT = 'black';
    const STROKE_FOCUSED = '#ffcc00';
    const LINK_DEFAULT = 'black';
    const LINK_FOCUSED = '#0066ff';

    const ZOOM_EXTENT = [0.1, 3];
    const TRANSITION_MS = 500;

    const linkGenerator = d3.linkVertical().x(d => d.x).y(d => d.y);

    const childrenOf = d =>
        [d.Left, d.Right].filter(child => child !== null && child !== undefined && child.Value !== null);

    const fillOf = color => (color === 'Red' ? FILL_RED : FILL_BLACK);

    function shiftSubtree(node, dx) {
        node.x += dx;

        if (node.children) {
            node.children.forEach(child => shiftSubtree(child, dx));
        }
    }

    function fixSingleChildPositions(node) {
        if (node.children && node.children.length === 1) {
            const child = node.children[0];

            if (node.data.Left && node.data.Left.Value === child.data.Value) {
                shiftSubtree(child, -SINGLE_CHILD_OFFSET);
            } else if (node.data.Right && node.data.Right.Value === child.data.Value) {
                shiftSubtree(child, SINGLE_CHILD_OFFSET);
            }
        }

        if (node.children) {
            node.children.forEach(fixSingleChildPositions);
        }
    }

    function layout(treeData) {
        const root = d3.hierarchy(treeData, childrenOf);
        d3.tree().nodeSize(NODE_SIZE)(root);
        fixSingleChildPositions(root);
        return root;
    }

    function createZoom(svg, group) {
        const zoom = d3.zoom()
            .scaleExtent(ZOOM_EXTENT)
            .on('zoom', event => group.attr('transform', event.transform));

        svg.call(zoom);
        return zoom;
    }

    function centerHorizontally(group, nodes, containerWidth) {
        const minX = d3.min(nodes, d => d.x);
        const maxX = d3.max(nodes, d => d.x);
        const offsetX = (containerWidth - (maxX - minX)) / 2 - minX;

        group.attr('transform', `translate(${offsetX}, ${TOP_MARGIN})`);
    }

    function focusOn(svg, zoom, point, view, options) {
        const scale = options.scale;
        const transform = d3.zoomIdentity
            .translate(
                view.width / 2 - point.x * scale,
                view.height / options.verticalDivisor - point.y * scale)
            .scale(scale);

        if (options.duration > 0) {
            return svg.transition().duration(options.duration).call(zoom.transform, transform);
        }

        svg.call(zoom.transform, transform);
        return null;
    }

    function drawLinks(group, links, isFocused) {
        group.selectAll('.link').remove();

        const selection = group.selectAll('.link')
            .data(links)
            .enter()
            .append('path')
            .attr('class', 'link')
            .attr('d', linkGenerator);

        if (isFocused) {
            selection
                .style('fill', 'none')
                .style('stroke', d => (isFocused(d.sourceId, d.targetId) ? LINK_FOCUSED : LINK_DEFAULT))
                .style('stroke-width', d => (isFocused(d.sourceId, d.targetId) ? '4px' : '2.2px'))
                .style('opacity', d => (d.opacity === undefined ? null : d.opacity));
        }

        return selection;
    }

    function linksOf(root) {
        return root.links().map(link => ({
            source: { x: link.source.x, y: link.source.y },
            target: { x: link.target.x, y: link.target.y },
            sourceId: link.source.data.NodeId,
            targetId: link.target.data.NodeId
        }));
    }

    function nodesOf(root) {
        return root.descendants().map(node => ({
            id: node.data.NodeId,
            x: node.x,
            y: node.y,
            value: node.data.Value,
            color: node.data.Color,
            opacity: 1
        }));
    }

    function drawNodes(group, items, options) {
        const opts = options || {};
        const isFocused = opts.isFocused || (() => false);
        const inline = opts.inlineStyles !== false;

        const groups = group.selectAll('.node')
            .data(items, d => d.id)
            .enter()
            .append('g')
            .attr('class', 'node')
            .attr('transform', d => `translate(${d.x},${d.y})`);

        if (inline) {
            groups.style('opacity', d => d.opacity);
        }

        groups.filter(d => isFocused(d.id))
            .append('circle')
            .attr('class', 'glow-effect')
            .attr('r', RADIUS_GLOW)
            .style('fill', 'none')
            .style('stroke', STROKE_FOCUSED)
            .style('stroke-width', '3px')
            .style('opacity', 0.8);

        const circles = groups.append('circle')
            .attr('r', d => (inline && isFocused(d.id) ? RADIUS_FOCUSED : RADIUS))
            .style('fill', d => (opts.fillOverride && opts.fillOverride(d)) || fillOf(d.color));

        if (inline) {
            circles
                .style('stroke', d => (isFocused(d.id) ? STROKE_FOCUSED : STROKE_DEFAULT))
                .style('stroke-width', d => (isFocused(d.id) ? '3px' : '2px'));
        }

        const texts = groups.append('text')
            .attr('dy', '0.35em')
            .attr('text-anchor', 'middle')
            .text(d => d.value);

        if (inline) {
            texts
                .style('font-weight', d => (isFocused(d.id) ? 'bold' : 'normal'))
                .style('font-size', d => (isFocused(d.id) ? '18px' : '16px'))
                .style('fill', 'white');
        }

        return groups;
    }

    function pulse(selection) {
        selection.select('circle')
            .transition()
            .duration(TRANSITION_MS)
            .attr('r', RADIUS_GLOW)
            .style('fill', FILL_PULSE)
            .transition()
            .duration(TRANSITION_MS)
            .attr('r', RADIUS)
            .style('fill', d => fillOf(d.color !== undefined ? d.color : d.data.Color));
    }

    return {
        TOP_MARGIN,
        layout,
        linksOf,
        nodesOf,
        createZoom,
        centerHorizontally,
        focusOn,
        drawLinks,
        drawNodes,
        pulse,
        fillOf
    };
})();
