import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

/**
 * Creating a sidebar enables you to:
 - create an ordered group of docs
 - render a sidebar for each doc of that group
 - provide next/previous navigation

 The sidebars can be generated from the filesystem, or explicitly defined here.

 Create as many sidebars as you want.
 */
const sidebars: SidebarsConfig = {
  docs: [
    'intro',
    'api-guide',
    'toon-spec',
    {
      type: 'category',
      label: 'Getting Started',
      collapsible: true,
      collapsed: false,
      items: [
        'getting-started/installation',
        'getting-started/quick-start',
        'getting-started/basic-serialization',
      ],
    },
    {
      type: 'category',
      label: 'Core Features',
      collapsible: true,
      collapsed: false,
      items: [
        'core-features/serialization',
        'core-features/deserialization',
        'core-features/streaming',
        'core-features/type-system',
        'core-features/configuration',
      ],
    },
    {
      type: 'category',
      label: 'Format Extensions',
      collapsible: true,
      collapsed: true,
      items: [
        'format-extensions/json-integration',
        'format-extensions/yaml-integration',
        'format-extensions/custom-formats',
      ],
    },
    {
      type: 'category',
      label: 'ASP.NET Core',
      collapsible: true,
      collapsed: true,
      items: [
        'aspnet-core/dependency-injection',
        'aspnet-core/input-formatters',
        'aspnet-core/output-formatters',
        'aspnet-core/configuration-provider',
      ],
    },
    {
      type: 'category',
      label: 'Advanced',
      collapsible: true,
      collapsed: true,
      items: [
        'advanced/performance-tuning',
        'advanced/custom-converters',
        'advanced/source-generators',
      ],
    },
  ],
};

export default sidebars;
