import type {ReactNode} from 'react';
import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  icon: string;
  description: ReactNode;
};

const FeatureList: FeatureItem[] = [
  {
    title: 'TOON Spec v4.4',
    icon: '📐',
    description: (
      <>
        Implements <strong>TOON spec v4.4</strong>, passes all 564 official conformance
        fixtures and writes the same text as the official .NET library. TOON is a compact,
        human-readable format that suits LLM prompts, configuration files and data exchange.
      </>
    ),
  },
  {
    title: 'Familiar API',
    icon: '🔧',
    description: (
      <>
        A <strong>System.Text.Json-style</strong> <code>ToonSerializer</code> with options,
        attributes and custom converters, plus a <code>ToonDocument</code>/<code>ToonValue</code>{' '}
        model for working with TOON directly. Builds for .NET 8 and .NET 10.
      </>
    ),
  },
  {
    title: 'Source Generator',
    icon: '⚙️',
    description: (
      <>
        Mark a partial type with <code>[ToonSerializable]</code> and the generator adds
        static <code>Serialize</code>/<code>Deserialize</code> methods.{' '}
        <code>ToonSerializer</code> uses the generated code for those types automatically.
      </>
    ),
  },
  {
    title: 'JSON and YAML',
    icon: '🔄',
    description: (
      <>
        Convert between TOON and JSON with <code>ToonConvert</code> (System.Text.Json), and
        between TOON and YAML with <code>ToonYamlConvert</code> (YamlDotNet).
      </>
    ),
  },
  {
    title: 'ASP.NET Core',
    icon: '🌐',
    description: (
      <>
        MVC input and output formatters for <code>application/toon</code> and <code>text/toon</code>, dependency
        injection registration with <code>AddToonNet</code>, and a TOON configuration
        provider with <code>AddToonFile</code>.
      </>
    ),
  },
  {
    title: 'Async and Streaming',
    icon: '📦',
    description: (
      <>
        Async APIs for strings, streams and files, and multi-document streaming that writes
        large collections in batches and reads them back one item at a time with{' '}
        <code>SerializeDocumentsAsync</code> and <code>DeserializeDocumentsAsync</code>.
      </>
    ),
  },
];

function Feature({title, icon, description}: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center" style={{fontSize: '4rem', marginBottom: '1rem'}}>
        {icon}
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
