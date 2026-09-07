/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
    "../blueward-design-system/src/**/*.{js,ts,jsx,tsx}",
    "../blueward-design-system/dist/**/*.{js,cjs}"
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        bds: {
          primary: '#2563EB',
          'primary-hover': '#1D4ED8',
          'primary-light': '#EFF6FF',
          dark: '#0F172A',
          card: '#1E293B'
        }
      }
    },
  },
  plugins: [],
};
