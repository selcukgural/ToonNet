# Website

This website is built using [Docusaurus](https://docusaurus.io/), a modern static website generator.

The documentation pages live in `docs/`; the sidebar is defined in `sidebars.ts` and the site settings in
`docusaurus.config.ts`. Requires Node.js 20 or later (CI uses Node.js 22).

## Installation

```bash
npm ci
```

## Local Development

```bash
npm start
```

This command starts a local development server and opens up a browser window. Most changes are reflected live without having to restart the server.

## Build

```bash
npm run build
```

This command generates static content into the `build` directory and can be served using any static contents hosting service. Use `npm run serve` to preview the build locally.

## Deployment

The site is deployed to GitHub Pages by the [`docs.yml`](../.github/workflows/docs.yml) workflow on every push to `master` that changes `website/` (it can also be started manually from the Actions tab). The [`ci.yml`](../.github/workflows/ci.yml) workflow builds the site on pull requests to catch broken builds; it does not deploy.

No manual deployment step is needed.
