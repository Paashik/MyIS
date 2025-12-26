import React, { useEffect, useMemo, useState } from "react";
import { Alert, Button, Card, Form, Spin, Typography } from "antd";
import { useLocation, useNavigate, useParams } from "react-router-dom";

import {
  CreateRequestPayload,
  RequestDto,
  RequestLineInputDto,
  RequestTypeDto,
  UpdateRequestPayload,
} from "../api/types";
import {
  createRequest,
  getRequest,
  getRequestTypes,
  updateRequest,
} from "../api/requestsApi";
import { RequestForm, RequestFormValues } from "../components/RequestForm";
import { t } from "../../../core/i18n/t";
import { CommandBar } from "../../../components/ui/CommandBar";

const { Title } = Typography;

type PageState =
  | { kind: "loading" }
  | { kind: "loaded" }
  | { kind: "error"; message: string };

type RequestsDirectionSegment = "incoming" | "outgoing";

export const RequestEditPage: React.FC = () => {
  const { id } = useParams<{ id?: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();
  const location = useLocation();

  const returnContext = (() => {
    const sp = new URLSearchParams(location.search);

    const rawDirection = (sp.get("direction") || "").trim().toLowerCase();
    const direction: RequestsDirectionSegment = rawDirection === "outgoing" ? "outgoing" : "incoming";

    const rawType = sp.get("type");
    const type = ((rawType || "").trim() || "all");

    const rawOnlyMine = (sp.get("onlyMine") || "").trim().toLowerCase();
    const onlyMine = rawOnlyMine === "1" || rawOnlyMine === "true";

    return { direction, type, onlyMine };
  })();

  const direction: RequestsDirectionSegment = returnContext.direction;

  const typeKeyFromQuery = (() => {
    const sp = new URLSearchParams(location.search);
    return (sp.get("type") || "").trim();
  })();

  const [state, setState] = useState<PageState>({ kind: "loading" });
  const [requestTypes, setRequestTypes] = useState<RequestTypeDto[]>([]);
  const [request, setRequest] = useState<RequestDto | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [form] = Form.useForm();

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setState({ kind: "loading" });

      try {
        const typesPromise = getRequestTypes();
        const requestPromise = isEdit && id ? getRequest(id) : Promise.resolve(null);

        const [types, existing] = await Promise.all([typesPromise, requestPromise]);

        if (cancelled) return;

        setRequestTypes(types);
        if (existing) {
          setRequest(existing as RequestDto);
        }

        setState({ kind: "loaded" });
      } catch (error) {
        if (cancelled) return;

        const message =
          error instanceof Error
            ? error.message
            : t("requests.edit.error.loadFormData");
        setState({ kind: "error", message });
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [id, isEdit]);

  const requestTypesForDirection = useMemo(() => {
    const expected = direction === "incoming" ? "Incoming" : "Outgoing";
    return requestTypes.filter((t) => t.direction === expected);
  }, [direction, requestTypes]);

  const preselectedTypeId = useMemo(() => {
    if (!typeKeyFromQuery || typeKeyFromQuery === "all") return undefined;
    const found = requestTypesForDirection.find((x) => x.id === typeKeyFromQuery);
    return found ? found.id : undefined;
  }, [requestTypesForDirection, typeKeyFromQuery]);

  const handleCancel = () => {
    if (isEdit && id) {
      navigate(
        `/requests/${encodeURIComponent(id)}?direction=${encodeURIComponent(returnContext.direction)}&type=${encodeURIComponent(returnContext.type)}${returnContext.onlyMine ? "&onlyMine=1" : ""}`
      );
    } else {
      const typeParam = typeKeyFromQuery && typeKeyFromQuery !== "all" ? typeKeyFromQuery : "all";
      const sp = new URLSearchParams();
      sp.set("direction", direction);
      sp.set("type", typeParam);
      if (returnContext.onlyMine) sp.set("onlyMine", "1");
      navigate(`/requests/journal?${sp.toString()}`);
    }
  };

  const handleSubmit = async (values: RequestFormValues) => {
    setSubmitting(true);
    setSaveError(null);

    try {
      if (isEdit && id) {
        const payload: UpdateRequestPayload = {
          title: values.title,
          description: values.description,
          lines: values.lines,
          dueDate: values.dueDate,
          relatedEntityType: values.relatedEntityType,
          relatedEntityId: values.relatedEntityId,
          relatedEntityName: values.relatedEntityName,
          externalReferenceId: values.externalReferenceId,
          targetEntityType: values.targetEntityType,
          targetEntityId: values.targetEntityId,
          targetEntityName: values.targetEntityName,
        };

        const updated = await updateRequest(id, payload);
        navigate(
          `/requests/${encodeURIComponent(updated.id)}?direction=${encodeURIComponent(returnContext.direction)}&type=${encodeURIComponent(returnContext.type)}${returnContext.onlyMine ? "&onlyMine=1" : ""}`,
          { replace: true }
        );
      } else {
        const payload: CreateRequestPayload = {
          requestTypeId: values.requestTypeId,
          title: values.title,
          description: values.description,
          lines: values.lines,
          dueDate: values.dueDate,
          relatedEntityType: values.relatedEntityType,
          relatedEntityId: values.relatedEntityId,
          relatedEntityName: values.relatedEntityName,
          externalReferenceId: values.externalReferenceId,
          targetEntityType: values.targetEntityType,
          targetEntityId: values.targetEntityId,
          targetEntityName: values.targetEntityName,
        };

        const created = await createRequest(payload);
        const typeParam = values.requestTypeId || "all";
        navigate(
          `/requests/${encodeURIComponent(created.id)}?direction=${encodeURIComponent(direction)}&type=${encodeURIComponent(typeParam)}${returnContext.onlyMine ? "&onlyMine=1" : ""}`,
          { replace: true }
        );
      }
    } catch (error) {
      const message =
        error instanceof Error ? error.message : t("requests.edit.error.save");
      setSaveError(message);
    } finally {
      setSubmitting(false);
    }
  };

  if (state.kind === "loading") {
    return (
      <div
        data-testid="request-edit-loading"
        style={{
          minHeight: "40vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <Spin
          tip={
            isEdit ? t("requests.edit.loading.edit") : t("requests.edit.loading.create")
          }
        />
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div>
        <Alert
          data-testid="request-edit-error-alert"
          type="error"
          message={
            isEdit
              ? t("requests.edit.error.load.title")
              : t("requests.edit.error.prepare.title")
          }
          description={state.message}
          showIcon
          style={{ marginBottom: 16 }}
        />
        <Button data-testid="request-edit-back-button" onClick={handleCancel}>
          {t("common.actions.back")}
        </Button>
      </div>
    );
  }

  if (isEdit && !request) {
    return (
      <Alert
        data-testid="request-edit-not-found-alert"
        type="error"
        message={t("requests.edit.error.notFound.title")}
        description={t("requests.edit.error.notFound.description")}
        showIcon
      />
    );
  }

  const initialValues =
    isEdit && request
      ? {
          requestTypeId: request.requestTypeId,
          title: request.title,
          description: request.description ?? "",
          lines: (request.lines ?? []).map(
            (l, idx): RequestLineInputDto => ({
              lineNo: idx + 1,
              description: l.description ?? undefined,
              quantity: l.quantity,
              needByDate: l.needByDate ?? undefined,
              supplierName: l.supplierName ?? undefined,
              supplierContact: l.supplierContact ?? undefined,
            })
          ),
          dueDate: request.dueDate ?? undefined,
          relatedEntityType: request.relatedEntityType ?? "",
          relatedEntityId: request.relatedEntityId ?? "",
          relatedEntityName: request.relatedEntityName ?? "",
          externalReferenceId: request.externalReferenceId ?? "",
          targetEntityType: request.targetEntityType ?? "",
          targetEntityId: request.targetEntityId ?? "",
          targetEntityName: request.targetEntityName ?? "",
        }
      : {
          requestTypeId: preselectedTypeId,
          title: "",
          description: "",
          lines: [],
          dueDate: undefined,
          relatedEntityType: "",
          relatedEntityId: "",
          relatedEntityName: "",
          externalReferenceId: "",
          targetEntityType: "",
          targetEntityId: "",
          targetEntityName: "",
        };

  const requestTypesForForm = isEdit ? requestTypes : requestTypesForDirection;

  return (
    <div data-testid="request-edit-page">
      <CommandBar
        left={
          <Title level={2} style={{ margin: 0 }}>
            {isEdit ? t("requests.edit.title.edit") : t("requests.edit.title.create")}
          </Title>
        }
        right={
          <>
            <Button data-testid="request-edit-cancel" onClick={handleCancel}>
              {t("common.actions.cancel")}
            </Button>
            <Button
              data-testid="request-edit-save"
              type="primary"
              loading={submitting}
              onClick={() => form.submit()}
            >
              {isEdit ? t("common.actions.save") : t("common.actions.create")}
            </Button>
          </>
        }
      />

      {saveError && (
        <Alert
          data-testid="request-edit-save-error-alert"
          type="error"
          message={t("requests.edit.error.save")}
          description={saveError}
          showIcon
          style={{ marginBottom: 16 }}
        />
      )}

      <Card data-testid="request-edit-card">
        <RequestForm
          mode={isEdit ? "edit" : "create"}
          initialValues={initialValues}
          requestTypes={requestTypesForForm}
          form={form}
          showActions={false}
          submitting={submitting}
          onSubmit={handleSubmit}
          onCancel={handleCancel}
        />
      </Card>
    </div>
  );
};
