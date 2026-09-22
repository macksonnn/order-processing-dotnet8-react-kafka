import { Button } from "../atoms/Button";

type PaginationProps = {
  page: number;
  totalPages: number;
  onPrevious: () => void;
  onNext: () => void;
};

export function Pagination({ page, totalPages, onPrevious, onNext }: PaginationProps) {
  return (
    <div className="row">
      <Button disabled={page <= 1} onClick={onPrevious}>
        Anterior
      </Button>
      <span>
        Página {page} de {totalPages}
      </span>
      <Button disabled={page >= totalPages} onClick={onNext}>
        Próxima
      </Button>
    </div>
  );
}
