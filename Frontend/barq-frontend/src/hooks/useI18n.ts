import { useState, useEffect } from 'react';

interface I18nConfig {
  language: string;
  direction: 'ltr' | 'rtl';
  translations: Record<string, string>;
}

const RTL_LANGUAGES = ['ar', 'he', 'fa', 'ur'];

export function useI18n() {
  const [config, setConfig] = useState<I18nConfig>({
    language: 'en',
    direction: 'ltr',
    translations: {}
  });

  useEffect(() => {
    const savedLanguage = localStorage.getItem('preferred-language') || 'en';
    const direction = RTL_LANGUAGES.includes(savedLanguage) ? 'rtl' : 'ltr';
    
    setConfig({
      language: savedLanguage,
      direction,
      translations: {}
    });

    document.documentElement.dir = direction;
    document.documentElement.lang = savedLanguage;
  }, []);

  const changeLanguage = (language: string) => {
    const direction = RTL_LANGUAGES.includes(language) ? 'rtl' : 'ltr';
    
    setConfig(prev => ({
      ...prev,
      language,
      direction
    }));

    localStorage.setItem('preferred-language', language);
    document.documentElement.dir = direction;
    document.documentElement.lang = language;
  };

  const t = (key: string, fallback?: string) => {
    return config.translations[key] || fallback || key;
  };

  return {
    language: config.language,
    direction: config.direction,
    changeLanguage,
    t,
    isRTL: config.direction === 'rtl'
  };
}
