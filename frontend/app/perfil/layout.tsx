import Nav from "@/components/nav/nav";

export default function PerfilLayout({ children }: { children: React.ReactNode }) {
  return (
    <>
      <Nav />
      {children}
    </>
  );
}
