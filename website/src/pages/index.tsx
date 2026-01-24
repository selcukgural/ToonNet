import type {ReactNode} from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';
import Heading from '@theme/Heading';

import styles from './index.module.css';

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={clsx('hero hero--primary', styles.heroBanner)}>
      <div className="container">
        <Heading as="h1" className="hero__title">
          {siteConfig.title}
        </Heading>
        <p className="hero__subtitle">{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link
            className="button button--secondary button--lg"
            to="/docs/intro">
            Get Started 🚀
          </Link>
          <Link
            className="button button--outline button--secondary button--lg"
            to="/docs/api/intro"
            style={{marginLeft: '1rem'}}>
            API Reference 📚
          </Link>
        </div>
      </div>
    </header>
  );
}

function QuickExample() {
  return (
    <section style={{padding: '4rem 0', backgroundColor: 'var(--ifm-background-surface-color)'}}>
      <div className="container">
        <div className="row">
          <div className="col col--6">
            <Heading as="h2">Quick Example</Heading>
            <p>Get started with ToonNet in seconds. No configuration needed.</p>
            <pre style={{backgroundColor: 'var(--ifm-code-background)', padding: '1rem', borderRadius: '8px', overflow: 'auto'}}>
              <code className="language-csharp">{`// Install via NuGet
dotnet add package ToonNet.Core

// Serialize
var person = new Person 
{ 
  Name = "Alice", 
  Age = 30 
};
string toon = ToonSerializer.Serialize(person);

// Deserialize
var restored = ToonSerializer
  .Deserialize<Person>(toon);`}</code>
            </pre>
          </div>
          <div className="col col--6">
            <Heading as="h2">TOON Format Output</Heading>
            <p>Clean, readable, and token-efficient data format.</p>
            <pre style={{backgroundColor: 'var(--ifm-code-background)', padding: '1rem', borderRadius: '8px', overflow: 'auto'}}>
              <code className="language-toon">{`Name: Alice
Age: 30

// vs JSON (40% more tokens)
{
  "Name": "Alice",
  "Age": 30
}`}</code>
            </pre>
            <div style={{marginTop: '2rem'}}>
              <Link className="button button--primary" to="/docs/intro">
                Learn More →
              </Link>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

export default function Home(): ReactNode {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={`${siteConfig.title} - Documentation`}
      description="High-performance TOON format serialization for .NET. AI-optimized, token-efficient, developer-friendly.">
      <HomepageHeader />
      <main>
        <HomepageFeatures />
        <QuickExample />
      </main>
    </Layout>
  );
}
