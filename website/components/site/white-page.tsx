/** Witte pagina voor de bento-stijl: de footer en de navigatie worden hier witgemaakt. */
export function WhitePage({ children }: { children: React.ReactNode }) {
  return (
    <div className="bg-white [&_footer]:border-t-0 [&_footer]:bg-white [&_header]:bg-white/85">
      {children}
    </div>
  );
}
